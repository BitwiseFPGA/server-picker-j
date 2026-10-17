namespace ServerPickerX.Tests
{
    internal static class TestPaths
    {
        // Tests run from the build output folder, so walk up to the folder that holds the solution
        public static string FromRepositoryRoot(params string[] parts)
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);

            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "ServerPickerX.slnx")))
            {
                directory = directory.Parent;
            }

            Assert.NotNull(directory);

            return Path.Combine(new[] { directory.FullName }.Concat(parts).ToArray());
        }
    }
}
