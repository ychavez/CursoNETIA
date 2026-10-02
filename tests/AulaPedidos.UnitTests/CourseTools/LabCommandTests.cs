using AulaPedidos.CourseTools;

namespace AulaPedidos.UnitTests.CourseTools;

public sealed class LabCommandTests : IDisposable
{
    private readonly DirectoryInfo workspace = Directory.CreateTempSubdirectory("aulapedidos-lab-tests-");
    private readonly string reference;

    public LabCommandTests()
    {
        reference = Path.Combine(workspace.FullName, "reference");
        Directory.CreateDirectory(reference);
        File.WriteAllText(Path.Combine(reference, "reference.txt"), "Referencia sintética que no debe cambiar.");
    }

    [Fact]
    public async Task Existing_destination_is_rejected_without_touching_student_work_or_reading_project_sources()
    {
        var destination = Path.Combine(workspace.FullName, "existing-lab");
        Directory.CreateDirectory(destination);
        var work = Path.Combine(destination, "student-work.txt");
        const string original = "Trabajo del alumno que nunca se debe sobrescribir.";
        await File.WriteAllTextAsync(work, original);
        var before = SnapshotPaths();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            LabCommand.ExecuteAsync(reference, ["--destination", destination]));

        Assert.Equal(original, await File.ReadAllTextAsync(work));
        Assert.Equal("Referencia sintética que no debe cambiar.", await File.ReadAllTextAsync(Path.Combine(reference, "reference.txt")));
        Assert.Equal(before, SnapshotPaths());
    }

    [Theory]
    [InlineData(".")]
    [InlineData("new-lab")]
    [InlineData("nested/../new-lab")]
    public async Task Reference_or_internal_destination_is_rejected_before_creating_files(string destination)
    {
        var before = SnapshotPaths();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            LabCommand.ExecuteAsync(reference, ["--destination", destination]));

        Assert.Equal(before, SnapshotPaths());
        Assert.Equal("Referencia sintética que no debe cambiar.", await File.ReadAllTextAsync(Path.Combine(reference, "reference.txt")));
    }

    private string[] SnapshotPaths() => Directory.EnumerateFileSystemEntries(workspace.FullName, "*", SearchOption.AllDirectories)
        .Select(path => Path.GetRelativePath(workspace.FullName, path)).Order(StringComparer.Ordinal).ToArray();

    public void Dispose() => workspace.Delete(recursive: true);
}
