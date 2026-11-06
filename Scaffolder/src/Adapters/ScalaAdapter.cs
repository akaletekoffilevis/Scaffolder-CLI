using Scaffolder.Services;

namespace Scaffolder.Adapters;

public class ScalaAdapter : IAdapter
{
    public string Name => "scala";
    public string Description => "Projets Scala (sbt ou mill)";
    public string[] Languages => ["scala"];
    public string[] SubTemplates => ["scala", "scala-basic", "scala-akka", "scala-http4s"];
    public bool IsAvailable => ProcessService.CommandExists("sbt") || ProcessService.CommandExists("scala");

    public Task<(int ExitCode, string Message)> ScaffoldAsync(
        string name, string outputDir, string subTemplate, string? language)
    {
        name = name ?? throw new ArgumentNullException(nameof(name));
        ConsoleService.Info($"  Generation du projet Scala '{name}'...");
        Console.WriteLine();

        Directory.CreateDirectory(outputDir);

        if (subTemplate == "scala-akka" || subTemplate == "scala-http4s")
        {
            var srcDir = Path.Combine(outputDir, "src", "main", "scala", name.Replace("-", "."));
            Directory.CreateDirectory(srcDir);

            if (subTemplate == "scala-http4s")
            {
                File.WriteAllText(Path.Combine(outputDir, "build.sbt"),
                    "name := \"" + name + "\"\nversion := \"0.1.0\"\nscalaVersion := \"3.4.0\"\n\n" +
                    "libraryDependencies ++= Seq(\n" +
                    "  \"org.http4s\" %% \"http4s-dsl\" % \"0.23.27\",\n" +
                    "  \"org.http4s\" %% \"http4s-ember-server\" % \"0.23.27\",\n" +
                    "  \"org.http4s\" %% \"http4s-ember-client\" % \"0.23.27\"\n)\n");

                var objectName = name.Replace("-", "").Split('.').Last();
                File.WriteAllText(Path.Combine(srcDir, "Main.scala"),
                    "import cats.effect._\nimport org.http4s._\nimport org.http4s.implicits._\n" +
                    "import org.http4s.ember.server._\nimport com.comcast.ip4s._\n\n" +
                    "object " + objectName + " extends IOApp {\n" +
                    "  def run(args: List[String]): IO[ExitCode] =\n" +
                    "    EmberServer.default[IO]\n" +
                    "      .withHost(host\"0.0.0.0\")\n" +
                    "      .withPort(port\"8080\")\n" +
                    "      .withHttpApp(HttpRoutes.of[IO] {\n" +
                    "        case GET -> Root / \"hello\" => Ok(\"Hello, World!\")\n" +
                    "      }).orNotFound\n" +
                    "      .build\n" +
                    "      .useForever\n" +
                    "      .as(ExitCode.Success)\n}\n");
            }
            else
            {
                File.WriteAllText(Path.Combine(outputDir, "build.sbt"),
                    "name := \"" + name + "\"\nversion := \"0.1.0\"\nscalaVersion := \"3.4.0\"\n\n" +
                    "libraryDependencies ++= Seq(\n" +
                    "  \"com.typesafe.akka\" %% \"akka-actor-typed\" % \"2.9.7\",\n" +
                    "  \"com.typesafe.akka\" %% \"akka-stream\" % \"2.9.7\"\n)\n");

                var objectName = name.Replace("-", "").Split('.').Last();
                File.WriteAllText(Path.Combine(srcDir, "Main.scala"),
                    "import akka.actor.typed.ActorSystem\nimport akka.actor.typed.scaladsl.Behaviors\n\n" +
                    "object " + objectName + " {\n" +
                    "  def main(args: Array[String]): Unit = {\n" +
                    "    val greeterSystem = ActorSystem(Behaviors.empty[Any], \"" + name + "\")\n" +
                    "    greeterSystem.log.info(\"Hello, " + name + "!\")\n" +
                    "    greeterSystem.terminate()\n" +
                    "  }\n}\n");
            }

            File.WriteAllText(Path.Combine(outputDir, ".gitignore"),
                "# Scala\n.idea/\n.bsp/\ntarget/\nproject/target/\n");
            File.WriteAllText(Path.Combine(outputDir, "README.md"),
                "# " + name + "\n\nProject Scala (" + subTemplate + ").\n\n## Usage\n\n```bash\nsbt run\n```\n");

            return Task.FromResult((0, "Projet Scala '" + subTemplate + "' cree avec succes"));
        }

        // Basic scala template
        var basicSrcDir = Path.Combine(outputDir, "src", "main", "scala");
        Directory.CreateDirectory(basicSrcDir);

        File.WriteAllText(Path.Combine(outputDir, "build.sbt"),
            "name := \"" + name + "\"\nversion := \"0.1.0\"\nscalaVersion := \"3.4.0\"\n");

        var safeName = name.Replace("-", "_");
        File.WriteAllText(Path.Combine(basicSrcDir, "Main.scala"),
            "@main def " + safeName + "() =\n  println(\"Hello, " + name + "!\")\n");

        File.WriteAllText(Path.Combine(outputDir, ".gitignore"),
            "# Scala\n.idea/\n.bsp/\ntarget/\nproject/target/\n");
        File.WriteAllText(Path.Combine(outputDir, "README.md"),
            "# " + name + "\n\nProject Scala.\n\n## Usage\n\n```bash\nsbt run\n```\n");

        return Task.FromResult((0, "Projet Scala cree avec succes"));
    }
}
