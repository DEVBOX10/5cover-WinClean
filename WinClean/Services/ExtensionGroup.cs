﻿using System.Collections;

using Microsoft.Win32;

namespace Scover.WinClean.Services;

/// <summary>A group of related extensions.</summary>
public sealed class ExtensionGroup : IEnumerable<string>
{
    private readonly IEnumerable<string> _extensions;

    /// <param name="extensions">The extensions in the group.</param>
    public ExtensionGroup(IEnumerable<string> extensions) => _extensions = extensions;

    /// <inheritdoc cref="ExtensionGroup(IEnumerable{string})"/>
    public ExtensionGroup(params string[] extensions) : this((IEnumerable<string>)extensions)
    {
    }
    
    /// <summary>Gets an user-friendly name for this extension group from HKEY_CLASSES_ROOT.</summary>
    public string Name
        => (Registry.ClassesRoot.OpenSubKey(_extensions.First())?.GetValue(null) is string name
            ? Registry.ClassesRoot.OpenSubKey(name)?.GetValue(null) as string
            : null) ?? "";

    public IEnumerator<string> GetEnumerator() => _extensions.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_extensions).GetEnumerator();
}