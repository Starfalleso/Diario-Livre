using DiarioLivre.Core.Services;

namespace DiarioLivre.Core.Tests;

public class StudentBatchParserTests
{
    private readonly StudentBatchParser _parser = new();

    [Fact]
    public void Parse_PlainLines_ReturnsEntriesWithoutExplicitRollNumbers()
    {
        string input = "Ana Clara Silva\nBruno Costa\r\nCarla Oliveira\n";

        IReadOnlyList<ParsedStudentEntry> result = _parser.Parse(input);

        Assert.Equal(3, result.Count);
        Assert.Null(result[0].RollNumber);
        Assert.Equal("Ana Clara Silva", result[0].FullName);
        Assert.Null(result[1].RollNumber);
        Assert.Equal("Bruno Costa", result[1].FullName);
        Assert.Null(result[2].RollNumber);
        Assert.Equal("Carla Oliveira", result[2].FullName);
    }

    [Fact]
    public void Parse_NumberedFormats_ExtractsRollNumbersAndNames()
    {
        string input = """
            01 - Ana Clara Silva
            2. Bruno Costa
            03) Carla Oliveira
            4;Diego Lima
            5, Elena Souza
            6 Lucas Martins
            """;

        IReadOnlyList<ParsedStudentEntry> result = _parser.Parse(input);

        Assert.Equal(6, result.Count);
        Assert.Equal(new ParsedStudentEntry(1, "Ana Clara Silva"), result[0]);
        Assert.Equal(new ParsedStudentEntry(2, "Bruno Costa"), result[1]);
        Assert.Equal(new ParsedStudentEntry(3, "Carla Oliveira"), result[2]);
        Assert.Equal(new ParsedStudentEntry(4, "Diego Lima"), result[3]);
        Assert.Equal(new ParsedStudentEntry(5, "Elena Souza"), result[4]);
        Assert.Equal(new ParsedStudentEntry(6, "Lucas Martins"), result[5]);
    }

    [Fact]
    public void Parse_CsvWithHeader_SkipsHeaderAndPreservesRows()
    {
        string input = """
            Número;Nome Completo
            1;"Ana Clara Silva"
            2;"Bruno Costa"
            """;

        IReadOnlyList<ParsedStudentEntry> result = _parser.Parse(input);

        Assert.Equal(2, result.Count);
        Assert.Equal(new ParsedStudentEntry(1, "Ana Clara Silva"), result[0]);
        Assert.Equal(new ParsedStudentEntry(2, "Bruno Costa"), result[1]);
    }

    [Fact]
    public void Parse_NormalizesExtraWhitespace()
    {
        string input = "  07   -   Gabriel   Almeida   Santos  ";

        IReadOnlyList<ParsedStudentEntry> result = _parser.Parse(input);

        Assert.Single(result);
        Assert.Equal(7, result[0].RollNumber);
        Assert.Equal("Gabriel Almeida Santos", result[0].FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   \r\n  \t  ")]
    public void Parse_EmptyOrWhitespace_ReturnsEmpty(string? input)
    {
        IReadOnlyList<ParsedStudentEntry> result = _parser.Parse(input);

        Assert.Empty(result);
    }
}
