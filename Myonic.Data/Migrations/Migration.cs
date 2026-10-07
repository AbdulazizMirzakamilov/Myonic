namespace Myonic.Data.Migrations;

public sealed record Migration(int Version, string Description, IReadOnlyList<string> Statements);