using System.Xml.Linq;
using Dapper;
using FlexAgent.IdentityAccess.Application;
using FlexAgent.Postgres;
using Microsoft.AspNetCore.DataProtection.Repositories;

namespace FlexAgent.Api;

internal sealed class PostgresDataProtectionXmlRepository(
    PostgresConnectionAccessor connectionAccessor,
    ISymmetricPayloadProtector protector) : IXmlRepository
{
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var connection = connectionAccessor.DataSource.OpenConnection();
        var rows = connection.Query<byte[]>(
            "SELECT xml_ciphertext FROM data_protection_keys ORDER BY id;");
        return rows.Select(ciphertext => XElement.Parse(protector.Unprotect(ciphertext))).ToArray();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        using var connection = connectionAccessor.DataSource.OpenConnection();
        connection.Execute(
            """
            INSERT INTO data_protection_keys (friendly_name, xml_ciphertext)
            VALUES (@FriendlyName, @XmlCiphertext);
            """,
            new
            {
                FriendlyName = friendlyName,
                XmlCiphertext = protector.Protect(element.ToString(SaveOptions.DisableFormatting)),
            });
    }
}
