using FluentAssertions;
using SIMS.Domain.Entities;
using SIMS.Infrastructure.Repositories;

namespace SIMS.Tests;

public class CsvRepositoryTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "sims-csv-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_dir, "courses.csv");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static Course NewCourse(string code, string title = "Title", string description = "") => new()
    {
        Code = code,
        Title = title,
        Description = description,
        Credits = 3
    };

    [Fact]
    public async Task Add_PersistsToFile_AndNewInstanceReadsItBack()
    {
        var course = NewCourse("PRN211", "Applied Programming");
        await new CsvRepository<Course>(FilePath).AddAsync(course);

        var loaded = await new CsvRepository<Course>(FilePath).GetByIdAsync(course.Id);

        loaded.Should().NotBeNull();
        loaded!.Code.Should().Be("PRN211");
        loaded.Title.Should().Be("Applied Programming");
        loaded.Credits.Should().Be(3);
        loaded.CreatedAt.Should().Be(course.CreatedAt);
    }

    [Fact]
    public async Task Write_UsesHeaderRowAndCrlf()
    {
        await new CsvRepository<Course>(FilePath).AddAsync(NewCourse("A1"));

        var text = await File.ReadAllTextAsync(FilePath);
        var lines = text.Split("\r\n");

        lines[0].Split(',').Should().Contain(new[] { "Id", "Code", "Title" });
        lines.Should().HaveCount(3); // header, one row, trailing empty after final CRLF
        lines[2].Should().BeEmpty();
    }

    [Fact]
    public async Task SpecialCharacters_RoundTrip_PerRfc4180()
    {
        var tricky = "He said \"hi\", then\r\nleft\nnow";
        var course = NewCourse("X,1", "\"Quoted\"", tricky);
        await new CsvRepository<Course>(FilePath).AddAsync(course);

        var raw = await File.ReadAllTextAsync(FilePath);
        raw.Should().Contain("\"X,1\"");
        raw.Should().Contain("\"\"\"Quoted\"\"\"");

        var loaded = (await new CsvRepository<Course>(FilePath).GetAllAsync()).Single();
        loaded.Code.Should().Be("X,1");
        loaded.Title.Should().Be("\"Quoted\"");
        loaded.Description.Should().Be(tricky);
    }

    [Fact]
    public async Task Find_FiltersWithPredicate()
    {
        var repo = new CsvRepository<Course>(FilePath);
        await repo.AddRangeAsync(new[] { NewCourse("A"), NewCourse("B"), NewCourse("A") });

        var found = await repo.FindAsync(c => c.Code == "A");

        found.Should().HaveCount(2);
    }

    [Fact]
    public async Task Update_ReplacesRow_AndPersists()
    {
        var repo = new CsvRepository<Course>(FilePath);
        var course = NewCourse("A", "Old");
        await repo.AddAsync(course);

        course.Title = "New";
        repo.Update(course);

        (await new CsvRepository<Course>(FilePath).GetByIdAsync(course.Id))!.Title.Should().Be("New");
    }

    [Fact]
    public async Task Remove_DeletesRow_AndPersists()
    {
        var repo = new CsvRepository<Course>(FilePath);
        var keep = NewCourse("keep");
        var drop = NewCourse("drop");
        await repo.AddRangeAsync(new[] { keep, drop });

        repo.Remove(drop);

        var all = await new CsvRepository<Course>(FilePath).GetAllAsync();
        all.Select(c => c.Code).Should().Equal("keep");
    }

    [Fact]
    public async Task NullableValues_RoundTripAsNull()
    {
        var path = Path.Combine(_dir, "enrollments.csv");
        var withGrade = new Enrollment { Grade = 8.5 };
        var noGrade = new Enrollment { Grade = null };
        await new CsvRepository<Enrollment>(path).AddRangeAsync(new[] { withGrade, noGrade });

        var repo = new CsvRepository<Enrollment>(path);
        (await repo.GetByIdAsync(withGrade.Id))!.Grade.Should().Be(8.5);
        (await repo.GetByIdAsync(noGrade.Id))!.Grade.Should().BeNull();
    }

    [Fact]
    public async Task Add_DuplicateId_Throws()
    {
        var repo = new CsvRepository<Course>(FilePath);
        var course = NewCourse("A");
        await repo.AddAsync(course);

        var act = () => repo.AddAsync(course);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task MissingFile_ReturnsEmpty()
    {
        (await new CsvRepository<Course>(FilePath).GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task HandWrittenFile_WithoutTrailingNewline_AndEmptyFields_IsParsed()
    {
        Directory.CreateDirectory(_dir);
        var id = Guid.NewGuid();
        await File.WriteAllTextAsync(FilePath, "Id,Code,Title,Credits" + "\r\n" + id + ",PRN,,4");

        var course = (await new CsvRepository<Course>(FilePath).GetAllAsync()).Single();

        course.Id.Should().Be(id);
        course.Code.Should().Be("PRN");
        course.Title.Should().BeEmpty();
        course.Credits.Should().Be(4);
    }
}
