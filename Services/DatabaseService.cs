using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Group_2.Models;
using Microsoft.Maui.Storage;

namespace Group_2.Services
{
    // Simple JSON-backed persistence for development/demo purposes
    public static class DatabaseService
    {
        static readonly JsonSerializerOptions opts = new JsonSerializerOptions { WriteIndented = true };

        static string EventsFile => Path.Combine(FileSystem.AppDataDirectory, "events.json");
        static string UsersFile => Path.Combine(FileSystem.AppDataDirectory, "users.json");

        public static async Task<List<Event>> LoadEventsAsync()
        {
            try
            {
                if (!File.Exists(EventsFile)) return new();
                var json = await File.ReadAllTextAsync(EventsFile);
                return JsonSerializer.Deserialize<List<Event>>(json, opts) ?? new();
            }
            catch { return new(); }
        }

        public static async Task SaveEventsAsync(List<Event> items)
        {
            var json = JsonSerializer.Serialize(items, opts);
            await File.WriteAllTextAsync(EventsFile, json);
        }

        public static async Task<List<User>> LoadUsersAsync()
        {
            try
            {
                if (!File.Exists(UsersFile)) return new();
                var json = await File.ReadAllTextAsync(UsersFile);
                return JsonSerializer.Deserialize<List<User>>(json, opts) ?? new();
            }
            catch { return new(); }
        }

        public static async Task SaveUsersAsync(List<User> items)
        {
            var json = JsonSerializer.Serialize(items, opts);
            await File.WriteAllTextAsync(UsersFile, json);
        }
    }
}