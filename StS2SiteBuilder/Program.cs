var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
var distDir    = Path.Combine(projectDir, "dist");
Directory.CreateDirectory(distDir);

Console.WriteLine($"StS2SiteBuilder");
Console.WriteLine($"Output: {distDir}");
