﻿namespace Scover.WinClean.BusinessLogic.Scripts;

/// <summary>A collection of scripts created from files.</summary>
public sealed class FileScriptCollection : ScriptCollection, IMutableScriptCollection
{
    private readonly string _directory;
    private readonly string _scriptFileExtension;

    /// <summary></summary>
    /// <param name="directory">The directory containing the script files.</param>
    /// <param name="scriptFileExtension">The extension of the script files.</param>
    /// <param name="invalidScriptDataReloadElseIgnore">
    /// <inheritdoc cref="InvalidScriptDataCallback" path="/summary"/> Returns <inheritdoc cref="InvalidScriptDataCallback" path="/returns"/>
    /// </param>
    /// <param name="fsErrorReloadElseIgnore">
    /// <inheritdoc cref="FSErrorCallback" path="/summary"/> Returns <inheritdoc cref="FSErrorCallback" path="/returns"/>
    /// </param>
    /// <inheritdoc cref="ScriptCollection(IScriptSerializer, ScriptType)" path="/param"/>
    public FileScriptCollection(string directory,
                                string scriptFileExtension,
                                InvalidScriptDataCallback invalidScriptDataReloadElseIgnore,
                                FSErrorCallback fsErrorReloadElseIgnore,
                                IScriptSerializer serializer,
                                ScriptType scriptType) : base(serializer, scriptType)
    {
        (_directory, _scriptFileExtension) = (directory, scriptFileExtension);
        foreach (var filePath in Directory.EnumerateFiles(directory, $"*{scriptFileExtension}",
                     SearchOption.AllDirectories))
        {
        retry: 
            try
            {
                using Stream file = File.OpenRead(filePath);
                var script = Deserialize(file);
                Sources.Add(script, filePath);
            }
            catch (Exception e) when (e.IsFileSystem())
            {
                if (fsErrorReloadElseIgnore(e, FSVerb.Access, new FileInfo(filePath)))
                {
                    goto retry;
                }
            }
            catch (InvalidDataException e)
            {
                if (invalidScriptDataReloadElseIgnore(e, filePath))
                {
                    goto retry;
                }
            }
        }
    }

    public void Add(Script script)
    {
        string savingPath = Path.Join(_directory, script.InvariantName.ToFilename() + _scriptFileExtension);
        Sources.Add(script, savingPath);
    }

    public void Remove(Script script)
    {
        File.Delete(Sources[script]);
        _ = Sources.Remove(script);
    }

    public void Save()
    {
        foreach (Script s in this)
        {
            using Stream file = File.Create(Sources[s]);
            Serialize(s, file);
        }
    }
}