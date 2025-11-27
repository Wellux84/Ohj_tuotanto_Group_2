using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using Group_2.Models;
using System.Diagnostics;

namespace Group_2.Services
{
    public static class Database
    {
        // Päivitä SALASANA oikeaksi
        // HUOM: Jokaisen tiimin jäsenen tulee muokata tähän oma tietokantayhteys
        // 🔒 HUOM!
        // Älä lisää omaa salasanaa julkiseen GitHub-repoon.
        // Jokainen tiimin jäsen muokkaa tämän rivin omaan koneeseensa.
        // Esimerkki MariaDB-yhteydestä:
        // "Server=127.0.0.1;Port=3307;Database=tapahtumat;User Id=root;Password=omaSalasana;SslMode=None;CharSet=utf8mb4";

        public const string ConnString =
            "Server=127.0.0.1;Port=3307;Database=tapahtumat;User Id=root;Password=uusiSalasana;SslMode=None;CharSet=utf8mb4";
    }

    public static class DatabaseService
    {
        private static string FullError(Exception ex) => ex.ToString();

        public static async Task EnsureSchemaAsync()
        {
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();

            var stmts = new[]
            {
@"CREATE TABLE IF NOT EXISTS `kayttaja` (
  `user_id` CHAR(36) PRIMARY KEY,
  `nimi` VARCHAR(100) NOT NULL,
  `sahkoposti` VARCHAR(255) NOT NULL,
  UNIQUE KEY `sahkoposti` (`sahkoposti`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

@"CREATE TABLE IF NOT EXISTS `tapahtuma` (
  `event_id` CHAR(36) PRIMARY KEY,
  `otsikko` VARCHAR(200) NOT NULL,
  `kuvaus` TEXT DEFAULT NULL,
  `paivamaara` DATE NOT NULL,
  `loppupaivamaara` DATE NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;",

@"CREATE TABLE IF NOT EXISTS `ilmoittautuminen` (
  `event_id` CHAR(36) NOT NULL,
  `user_id` CHAR(36) NOT NULL,
  PRIMARY KEY(`event_id`,`user_id`),
  KEY `fk_ilmo_user` (`user_id`),
  CONSTRAINT `fk_ilmo_event` FOREIGN KEY(`event_id`) REFERENCES `tapahtuma`(`event_id`) ON DELETE CASCADE ON UPDATE CASCADE,
  CONSTRAINT `fk_ilmo_user` FOREIGN KEY(`user_id`) REFERENCES `kayttaja`(`user_id`) ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;"
            };

            foreach (var sql in stmts)
            {
                using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }

        // Quick helper to surface connection errors (returns null on success or exception text)
        public static async Task<string?> TestConnectionAsync()
        {
            try
            {
                using var conn = new MySqlConnection(Database.ConnString);
                await conn.OpenAsync();
                await conn.CloseAsync();
                return null;
            }
            catch (Exception ex)
            {
                return FullError(ex);
            }
        }

        // Helper: read Guid from reader even if underlying type is Guid or string
        private static Guid ReadGuid(MySqlDataReader rd, int index)
        {
            var val = rd.GetValue(index);
            if (val is Guid g) return g;
            if (val is string s) return Guid.Parse(s);
            // fallback
            return Guid.Parse(Convert.ToString(val) ?? Guid.Empty.ToString());
        }

        // ---------- USERS ----------
        public static async Task<List<User>> LoadUsersAsync()
        {
            var list = new List<User>();
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();
            const string sql = "SELECT `user_id`, `nimi`, `sahkoposti` FROM `kayttaja` ORDER BY `nimi`";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                list.Add(new User
                {
                    Id = ReadGuid(rd, 0),
                    Name = rd.GetString(1),
                    Email = rd.GetString(2)
                });
            }
            return list;
        }

        public static async Task SaveUsersAsync(List<User> users)
        {
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // EI ENÄÄ DELETE FROM kayttaja → ei tuhoa ilmoittautumisia CASCADElla

                const string upsert = @"
INSERT INTO `kayttaja` (`user_id`, `nimi`, `sahkoposti`)
VALUES (@Id, @Name, @Email)
ON DUPLICATE KEY UPDATE
  `nimi` = VALUES(`nimi`),
  `sahkoposti` = VALUES(`sahkoposti`);";

                foreach (var u in users)
                {
                    using var cmd = new MySqlCommand(upsert, conn, (MySqlTransaction)tx);
                    cmd.Parameters.AddWithValue("@Id", u.Id.ToString());
                    cmd.Parameters.AddWithValue("@Name", u.Name ?? "");
                    cmd.Parameters.AddWithValue("@Email", u.Email ?? "");

                    Debug.WriteLine("Executing SQL: " + cmd.CommandText);
                    foreach (MySqlParameter p in cmd.Parameters)
                        Debug.WriteLine($"Param: {p.ParameterName} = {p.Value}");

                    await cmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Debug.WriteLine("SaveUsersAsync exception: " + ex);
                throw;
            }
        }



        // ---------- EVENTS ----------
        public static async Task<List<Event>> LoadEventsAsync()
        {
            var list = new List<Event>();
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();

            const string sqlEvt = @"
SELECT
  t.`event_id`,
  t.`otsikko`,
  t.`kuvaus`,
  t.`paivamaara`,
  t.`loppupaivamaara`,
  GROUP_CONCAT(i.`user_id` SEPARATOR ',') AS user_ids
FROM `tapahtuma` t
LEFT JOIN `ilmoittautuminen` i ON t.`event_id` = i.`event_id`
GROUP BY t.`event_id`, t.`otsikko`, t.`kuvaus`, t.`paivamaara`, t.`loppupaivamaara`
ORDER BY t.`paivamaara` DESC;
";

            using (var cmd = new MySqlCommand(sqlEvt, conn))
            using (var rd = await cmd.ExecuteReaderAsync())
            {
                while (await rd.ReadAsync())
                {
                    var eventId = ReadGuid(rd, 0);
                    var title = rd.IsDBNull(1) ? string.Empty : rd.GetString(1);
                    var subtitle = rd.IsDBNull(2) ? string.Empty : rd.GetString(2);
                    var date = rd.GetDateTime(3); // alkupäivä
                    var endDate = rd.GetDateTime(4); // loppupäivä

                    List<Guid> participantIds = new();
                    if (!rd.IsDBNull(5)) // user_ids on nyt indeksissä 5
                    {
                        var joined = rd.GetString(5); // comma-separated ids
                        if (!string.IsNullOrWhiteSpace(joined))
                        {
                            var parts = joined.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                            foreach (var p in parts)
                            {
                                if (Guid.TryParse(p, out var g)) participantIds.Add(g);
                            }
                        }
                    }

                    list.Add(new Event
                    {
                        Id = eventId,
                        Title = title,
                        Subtitle = subtitle,
                        Date = date,
                        EndDate = endDate,          // tärkeä: talletetaan loppupäivä
                        ParticipantIds = participantIds
                    });
                }
            }

            return list;
        }


        public static async Task SaveEventsAsync(List<Event> events)
        {
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                // Yksittäiset komennot
                const string delEvent = "DELETE FROM `tapahtuma` WHERE `event_id` = @Id";

                const string insEvt = @"
INSERT INTO `tapahtuma`
(`event_id`, `otsikko`, `kuvaus`, `paivamaara`, `loppupaivamaara`)
VALUES (@Id, @Title, @Subtitle, @EventDate, @EndDate);";

                const string delJoinsForEvent = "DELETE FROM `ilmoittautuminen` WHERE `event_id` = @E";
                const string insMap = "INSERT INTO `ilmoittautuminen` (`event_id`, `user_id`) VALUES(@E,@U)";

                foreach (var e in events)
                {
                    // Poista vanha event-rivi
                    using (var cmdDelEvt = new MySqlCommand(delEvent, conn, (MySqlTransaction)tx))
                    {
                        cmdDelEvt.Parameters.AddWithValue("@Id", e.Id.ToString());
                        await cmdDelEvt.ExecuteNonQueryAsync();
                    }

                    // Lisää uusi event-rivi
                    using (var cmd = new MySqlCommand(insEvt, conn, (MySqlTransaction)tx))
                    {
                        cmd.Parameters.AddWithValue("@Id", e.Id.ToString());
                        cmd.Parameters.AddWithValue("@Title", e.Title ?? "");
                        cmd.Parameters.AddWithValue("@Subtitle", e.Subtitle ?? "");
                        cmd.Parameters.AddWithValue("@EventDate", e.Date.Date);
                        cmd.Parameters.AddWithValue("@EndDate", e.EndDate.Date);

                        Debug.WriteLine("Executing SQL: " + cmd.CommandText);
                        foreach (MySqlParameter p in cmd.Parameters)
                            Debug.WriteLine($"Param: {p.ParameterName} = {p.Value}");

                        await cmd.ExecuteNonQueryAsync();
                    }

                    // Poista vanhat ilmoittautumiset tälle eventille
                    using (var cmdDelJoins = new MySqlCommand(delJoinsForEvent, conn, (MySqlTransaction)tx))
                    {
                        cmdDelJoins.Parameters.AddWithValue("@E", e.Id.ToString());
                        await cmdDelJoins.ExecuteNonQueryAsync();
                    }

                    // Lisää uudet ilmoittautumiset
                    if (e.ParticipantIds != null)
                    {
                        foreach (var uId in e.ParticipantIds)
                        {
                            using var cmd = new MySqlCommand(insMap, conn, (MySqlTransaction)tx);
                            cmd.Parameters.AddWithValue("@E", e.Id.ToString());
                            cmd.Parameters.AddWithValue("@U", uId.ToString());

                            Debug.WriteLine("Executing SQL: " + cmd.CommandText);
                            foreach (MySqlParameter p in cmd.Parameters)
                                Debug.WriteLine($"Param: {p.ParameterName} = {p.Value}");

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                Debug.WriteLine("SaveEventsAsync exception: " + ex);
                throw;
            }
        }


        public static async Task<(long users, long events, long joins)> SelfTestCountsAsync()
        {
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();

            async Task<long> CountAsync(string table)
            {
                using var cmd = new MySqlCommand($"SELECT COUNT(*) FROM `{table}`", conn);
                var o = await cmd.ExecuteScalarAsync();
                return (o is long l) ? l : Convert.ToInt64(o);
            }

            var u = await CountAsync("kayttaja");
            var e = await CountAsync("tapahtuma");
            var j = await CountAsync("ilmoittautuminen");
            return (u, e, j);
        }

        // Add this helper method to DatabaseService (near other Load... methods)
        public static async Task<List<(Guid EventId, Guid UserId)>> LoadJoinsAsync()
        {
            var list = new List<(Guid, Guid)>();
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();
            const string sql = "SELECT `event_id`, `user_id` FROM `ilmoittautuminen`";
            using var cmd = new MySqlCommand(sql, conn);
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                Guid eId;
                Guid uId;
                var val0 = rd.GetValue(0);
                if (val0 is Guid g0) eId = g0; else eId = Guid.Parse(Convert.ToString(val0)!);
                var val1 = rd.GetValue(1);
                if (val1 is Guid g1) uId = g1; else uId = Guid.Parse(Convert.ToString(val1)!);
                list.Add((eId, uId));
            }
            return list;
        }

        public static async Task DeleteEventAsync(Guid eventId)
        {
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Voi riittää pelkkä tapahtuma, koska FK ON DELETE CASCADE hoitaa ilmoittautumiset
                // mutta tehdään selkeästi molemmat.

                // Poista ilmoittautumiset tälle eventille
                using (var cmdDelJoins = new MySqlCommand(
                           "DELETE FROM `ilmoittautuminen` WHERE `event_id` = @Id",
                           conn,
                           (MySqlTransaction)tx))
                {
                    cmdDelJoins.Parameters.AddWithValue("@Id", eventId.ToString());
                    await cmdDelJoins.ExecuteNonQueryAsync();
                }

                // Poista itse tapahtuma
                using (var cmdDelEvt = new MySqlCommand(
                           "DELETE FROM `tapahtuma` WHERE `event_id` = @Id",
                           conn,
                           (MySqlTransaction)tx))
                {
                    cmdDelEvt.Parameters.AddWithValue("@Id", eventId.ToString());
                    await cmdDelEvt.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        public static async Task DeleteUserAsync(Guid userId)
        {
            using var conn = new MySqlConnection(Database.ConnString);
            await conn.OpenAsync();

            using var tx = await conn.BeginTransactionAsync();
            try
            {
                // Poista ilmoittautumiset tältä käyttäjältä
                using (var cmdDelJoins = new MySqlCommand(
                           "DELETE FROM `ilmoittautuminen` WHERE `user_id` = @Id",
                           conn,
                           (MySqlTransaction)tx))
                {
                    cmdDelJoins.Parameters.AddWithValue("@Id", userId.ToString());
                    await cmdDelJoins.ExecuteNonQueryAsync();
                }

                // Poista käyttäjä
                using (var cmdDelUser = new MySqlCommand(
                           "DELETE FROM `kayttaja` WHERE `user_id` = @Id",
                           conn,
                           (MySqlTransaction)tx))
                {
                    cmdDelUser.Parameters.AddWithValue("@Id", userId.ToString());
                    await cmdDelUser.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

    }
}

