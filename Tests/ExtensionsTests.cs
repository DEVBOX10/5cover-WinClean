using System.Collections;
using System.Globalization;
using System.Xml;

using Humanizer;

using Scover.WinClean;
using Scover.WinClean.Model;

namespace Tests;

public sealed class ExtensionsTests
{
    private static readonly TestCaseData[] sumCases =
    {
        new(10.Hours(), new[]{ 10.Hours() }),
        new(10.Hours(), new[]{ 5.Hours(), 5.Hours() }),
        new(10.Hours(), new[]{ 5, 3, 2 }.Select(s => s.Hours())),
        new(10.Hours(), new[]{ 0.3, 1.7, 4.6, 3.4 }.Select(s => s.Hours()))
    };

    [TestCase("Test", "value", "<Tests><Test>value</Test></Tests>")]
    public void TestGetSingleNode(string name, string innerText, string xml)
    {
        XmlDocument doc = new();
        doc.LoadXml(xml);

        Assert.That(doc.GetSingleChildText(name), Is.EqualTo(innerText));
    }

    [TestCase("<Tests/>", "'Tests' has no child element named 'Test'.")]
    [TestCase("<Tests><Test/><Test/></Tests>", "'Tests' has 2 child elements named 'Test' but only one was expected.")]
    public void TestGetSingleNodeException(string xml, string exceptionMessage)
    {
        XmlDocument doc = new();
        doc.LoadXml(xml);

        var e = Assert.Throws<XmlException>(() => doc.GetSingleChildText("Test"));
        Assert.That(e!.Message, Is.EqualTo(exceptionMessage));
    }

    [TestCaseSource(typeof(ItemsEqualCases))]
    public void TestItemsEqual<T>(IEnumerable<T> e1, IEnumerable<T> e2, bool areEquals)
        => Assert.That(e1.ItemsEqual(e2), Is.EqualTo(areEquals));

    [TestCase("<Test>value</Test>", "", "value")]
    [TestCase("<Test xml:lang=\"fr\">valueFr</Test>", "fr", "valueFr")]
    [TestCase("<TestElement/>", "", "")]
    [TestCase("<TestElement xml:lang=\"fr\"/>", "fr", "")]
    [TestCase("<TestElement xml:lang=\"en-US\"/>", "en-US", "")]
    public void TestSetFromXml(string xml, string cultureName, string value)
    {
        XmlDocument doc = new();
        doc.LoadXml(xml);
        LocalizedString str = new();

        str.SetFromXml(doc.DocumentElement.NotNull());

        Assert.That(str.Single(), Is.EqualTo(KeyValuePair.Create(new CultureInfo(cultureName), value)));
    }

    [TestCaseSource(nameof(sumCases))]
    public void TestSum(TimeSpan total, IEnumerable<TimeSpan> elements)
        => Assert.That(total, Is.EqualTo(elements.Sum(ts => ts)));

    [TestCase(new object[] { new object[0] })]
    [TestCase(new object[] { new object?[] { null } })]
    [TestCase(new object[] { new object?[] { null, null, null } })]
    [TestCase(new object[] { new object?[] { null, "", 0, 10.9, null } })]
    public void TestWithoutNull(IEnumerable<object?> sequence)
    {
        var sequenceWithoutNull = sequence.WithoutNull();
        Assert.That(sequenceWithoutNull, Does.Not.Contains(null));
        Assert.That(sequenceWithoutNull.Count(), Is.EqualTo(sequence.Count() - sequence.Count(o => o is null)));
    }

    [TestCase("1.2.3")]
    [TestCase("1.2.3.4")]
    public void TestWithoutRevision(string version) => Assert.That(new Version(version).WithoutRevision().Revision, Is.EqualTo(-1));

    private sealed class ItemsEqualCases : IEnumerable<TestCaseData>
    {
        private static List<(int, string)> Abc => new()
        {
            (1, "a"),
            (2, "b"),
            (3, "c"),
        };

        private static List<(int, string)> Acb => new()
        {
            (1, "a"),
            (3, "c"),
            (2, "b"),
        };

        private static List<(int, object)> Obj => new()
        {
            (1, new()),
            (2, new()),
            (3, new()),
        };

        public IEnumerator<TestCaseData> GetEnumerator()
        {
            yield return new(Enumerable.Empty<object>(), Enumerable.Empty<object>(), true);
            yield return new(Abc, Abc, true);
            yield return new(Obj, Obj, false);
            yield return new(Abc, Enumerable.Empty<(int, string)>(), false);
            yield return new(Abc, Acb, true);
            yield return new(Obj, Enumerable.Empty<(int, object)>(), false);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}