using System.Reflection;
using FlexAgent.Evaluation.Infrastructure;

namespace FlexAgent.Evaluation.Tests.Infrastructure;

public sealed class PostgresEvaluationDurableWorkStoreSqlTests
{
    [Theory]
    [InlineData("BacklogSql")]
    [InlineData("ClaimOrganizationSql")]
    [InlineData("ClaimSql")]
    public void Composed_sql_fragments_preserve_token_boundaries(string fieldName)
    {
        var sql = GetSql(fieldName);

        Assert.DoesNotContain("organization_idinner", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("workINNER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("INNER JOIN evaluation_requests AS request", sql, StringComparison.Ordinal);
    }

    private static string GetSql(string fieldName)
    {
        var field = typeof(PostgresEvaluationDurableWorkStore).GetField(
            fieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        return Assert.IsType<string>(field.GetValue(null));
    }
}
