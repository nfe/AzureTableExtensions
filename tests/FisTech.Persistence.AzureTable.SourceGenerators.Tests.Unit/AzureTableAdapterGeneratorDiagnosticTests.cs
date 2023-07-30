using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

namespace FisTech.Persistence.AzureTable.SourceGenerators.UnitTests;

public class AzureTableAdapterGeneratorDiagnosticTests
{
    private const string ModelSource = """
        using System;

        namespace TestNamespace.Models;

        public class Movie
        {
            public string Id { get; set; }
        
            public string Title { get; set; }
        
            public char FirstLetterOfTitle { get; set; }
        
            public Genre Genre { get; set; }
        
            public string Director { get; set; }
        
            public DateTime ReleaseDate { get; set; }
        
            public byte? AgeRating { get; set; }
        
            public float? Rating { get; set; }
        
            public bool IsAvailable { get; set; }
        
            public short? SequelNumber { get; set; }
        
            public long? Budget { get; set; }
        
            public double? BoxOfficeRevenue { get; set; }
        
            public DateTimeOffset? LastUpdate { get; set; }
        
            public string? ETag { get; set; }
        }

        public enum Genre
        {
            Action,
            Adventure,
            Animation,
            Comedy,
            Crime,
            Documentary,
            Drama,
            Family,
            Fantasy,
            Historical,
            Horror,
            Musical,
            Mystery,
            Romance,
            ScienceFiction,
            Thriller,
            War,
            Western
        }
        """;

    [Fact]
    public async Task Generator_InvalidAbstractClass_ReturnsDiagnosticErrorAZTBGEN001()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            public abstract partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN001")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 31)
                        .WithMessage(
                            "Adapter class 'TestNamespace.Adapters.MovieAdapter' should not be abstract")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_InvalidGenericClass_ReturnsDiagnosticErrorAZTBGEN002()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            public partial class MovieAdapter<T> : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN002")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 22)
                        .WithMessage(
                            "Adapter class 'TestNamespace.Adapters.MovieAdapter<T>' does not support generic types")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_ClassIsNotPartial_ReturnsDiagnosticErrorAZTBGEN003()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            public class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN003")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 14)
                        .WithMessage(
                            "Adapter class 'TestNamespace.Adapters.MovieAdapter' must have to be partial")
                }
            }
        };

        await test.RunAsync();
    }

    [Theory]
    // Missing PartitionKeyAttribute
    [InlineData("[RowKey(nameof(Movie.Id))]", "PartitionKeyAttribute", "null")]
    // Missing RowKeyAttribute
    [InlineData("[PartitionKey(nameof(Movie.Director))]", "RowKeyAttribute", "null")]
    // Nonexistent PartitionKeyAttribute
    [InlineData("[PartitionKey(\"MyProperty\")]", "PartitionKeyAttribute", "MyProperty")]
    public async Task Generator_PropertyNotFound_ReturnsDiagnosticErrorAZTBGEN004(string attributeSource,
        string attributeName, string propertyName)
    {
        var adapterSource = $$"""
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            {{attributeSource}}
            public partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN004")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 7, 22)
                        .WithMessage(
                            $"Property '{propertyName}' not found for '{attributeName}' on adapter class 'TestNamespace.Adapters.MovieAdapter'")
                }
            }
        };

        await test.RunAsync();
    }

    [Theory]
    // PartitionKeyAttribute with invalid property type
    [InlineData("""
        [PartitionKey(nameof(Movie.Rating))]
        [RowKey(nameof(Movie.Id))]
        [Timestamp(nameof(Movie.LastUpdate))]
        [ETag(nameof(Movie.ETag))]
        """, "PartitionKeyAttribute", "string")]
    // RowKeyAttribute with invalid property type
    [InlineData("""
        [PartitionKey(nameof(Movie.Director))]
        [RowKey(nameof(Movie.Rating))]
        [Timestamp(nameof(Movie.LastUpdate))]
        [ETag(nameof(Movie.ETag))]
        """, "RowKeyAttribute", "string")]
    // TimestampAttribute with invalid property type
    [InlineData("""
        [PartitionKey(nameof(Movie.Director))]
        [RowKey(nameof(Movie.Id))]
        [Timestamp(nameof(Movie.Rating))]
        [ETag(nameof(Movie.ETag))]
        """, "TimestampAttribute", "System.DateTimeOffset? or System.DateTimeOffset")]
    // ETagAttribute with invalid property type
    [InlineData("""
        [PartitionKey(nameof(Movie.Director))]
        [RowKey(nameof(Movie.Id))]
        [Timestamp(nameof(Movie.LastUpdate))]
        [ETag(nameof(Movie.Rating))]
        """, "ETagAttribute", "string? or string")]
    public async Task Generator_PropertyTypeMismatch_ReturnsDiagnosticErrorAZTBGEN005(string attributesSource,
        string attributeName, string propertyType)
    {
        var adapterSource = $$"""
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            {{attributesSource}}
            public partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN005")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 10, 22)
                        .WithMessage(
                            $"'{attributeName}' attribute must be of type '{propertyType}' on adapter class 'TestNamespace.Adapters.MovieAdapter'")
                }
            }
        };

        await test.RunAsync();
    }

    [Theory]
    [InlineData("decimal")]
    [InlineData("decimal?")]
    [InlineData("System.Uri")]
    [InlineData("NestedClass", "TestNamespace.Models.NestedClass")]
    public async Task Generator_UnsupportedPropertyType_ReturnsDiagnosticErrorAZTBGEN006(string propertyType,
        string? displayName = null)
    {
        var modelSource = $$"""
            namespace TestNamespace.Models;

            public class TestModel
            {
                public string PartitionKey { get; set; }
            
                public string RowKey { get; set; }
            
                public {{propertyType}} MyProperty { get; set; }
            }

            public class NestedClass
            {
                public string NestedProperty { get; set; }
            }
            """;

        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(TestModel.PartitionKey))]
            [RowKey(nameof(TestModel.RowKey))]
            public partial class TestModelAdapter : AzureTableAdapterBase<TestModel> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { modelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN006")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 8, 22)
                        .WithMessage(
                            $"Adapter class 'TestNamespace.Adapters.TestModelAdapter' does not support type '{displayName ?? propertyType}' for property 'MyProperty'")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_DuplicateNameChangeProperty_ReturnsDiagnosticErrorAZTBGEN007()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            [NameChange(nameof(Movie.BoxOfficeRevenue), "Revenue")]
            [NameChange(nameof(Movie.BoxOfficeRevenue), "Profit")]
            public partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN007")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 10, 22)
                        .WithMessage(
                            "Duplicate name change for property 'BoxOfficeRevenue' on adapter class 'TestNamespace.Adapters.MovieAdapter'")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_DuplicateNameChangeTargetName_ReturnsDiagnosticErrorAZTBGEN008()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            [NameChange(nameof(Movie.Budget), "FinancialPlan")]
            [NameChange(nameof(Movie.BoxOfficeRevenue), "FinancialPlan")]
            public partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN008")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 10, 22)
                        .WithMessage(
                            "A name change with the same target 'FinancialPlan' has already been added on adapter class 'TestNamespace.Adapters.MovieAdapter'")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_NameChangeTargetNameConflict_ReturnsDiagnosticErrorAZTBGEN009()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            [NameChange(nameof(Movie.Budget), "BoxOfficeRevenue")]
            public partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN009")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 9, 22)
                        .WithMessage(
                            "The name change target 'BoxOfficeRevenue' conflicts with an existing property on adapter class 'TestNamespace.Adapters.MovieAdapter'")
                }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task Generator_InvalidNameChangeTargetName_ReturnsDiagnosticErrorAZTBGEN010()
    {
        const string adapterSource = """
            using FisTech.Persistence.AzureTable;
            using TestNamespace.Models;

            namespace TestNamespace.Adapters;

            [PartitionKey(nameof(Movie.Director))]
            [RowKey(nameof(Movie.Id))]
            [NameChange(nameof(Movie.Budget), "   ")]
            public partial class MovieAdapter : AzureTableAdapterBase<Movie> { }
            """;

        var test = new AzureTableAdapterGeneratorTest
        {
            TestState =
            {
                Sources = { ModelSource, adapterSource },
                ExpectedDiagnostics =
                {
                    DiagnosticResult.CompilerError("AZTBGEN010")
                        .WithSeverity(DiagnosticSeverity.Error)
                        .WithLocation("/0/Test1.cs", 9, 22)
                        .WithMessage(
                            "Target name is not valid for property 'Budget' on adapter class 'TestNamespace.Adapters.MovieAdapter'")
                }
            }
        };

        await test.RunAsync();
    }
}