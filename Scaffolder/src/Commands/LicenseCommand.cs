using System.CommandLine;
using Scaffolder.Services;

namespace Scaffolder.Commands;

public class LicenseCommand : Command
{
    private static readonly (string Id, string Name, string Body)[] Licenses =
    [
        ("mit", "MIT (recommandee)", """
MIT License

Copyright (c) {year} {author}

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
"""),
        ("apache", "Apache 2.0", """
                                 Apache License
                           Version 2.0, January 2004
                        http://www.apache.org/licenses/

   TERMS AND CONDITIONS FOR USE, REPRODUCTION, AND DISTRIBUTION
   ...
"""),
        ("gpl3", "GNU GPL v3", """
                    GNU GENERAL PUBLIC LICENSE
                       Version 3, 29 June 2007

 Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 Everyone is permitted to copy and distribute verbatim copies
 of this license document, but changing it is not allowed.
"""),
        ("bsd2", "BSD 2-Clause", """
BSD 2-Clause License

Copyright (c) {year}, {author}
All rights reserved.
"""),
        ("isc", "ISC", """
ISC License

Copyright (c) {year} {author}

Permission to use, copy, modify, and/or distribute this software...
"""),
        ("unlicense", "Unlicense (domaine public)", """
This is free and unencumbered software released into the public domain.

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software...
"""),
    ];

    public LicenseCommand() : base("license", "Genere un fichier de licence")
    {
        var typeArg = new Argument<string>("type")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Type de licence (mit, apache, gpl3, bsd2, isc, unlicense)"
        };
        var authorOpt = new Option<string>("--author")
        {
            Description = "Nom du titulaire du copyright"
        };
        var yearOpt = new Option<string>("--year")
        {
            Description = "Annee du copyright"
        };
        var outputOpt = new Option<DirectoryInfo?>("--output")
        {
            Description = "Dossier de sortie (defaut: dossier courant)"
        };
        Add(typeArg);
        Add(authorOpt);
        Add(yearOpt);
        Add(outputOpt);
        SetAction((ParseResult pr) => HandleLicense(
            pr.GetValue(typeArg), pr.GetValue(authorOpt),
            pr.GetValue(yearOpt), pr.GetValue(outputOpt)));
    }

    private static int HandleLicense(string? type, string? author, string? year, DirectoryInfo? output)
    {
        if (string.IsNullOrWhiteSpace(type))
        {
            ConsoleService.Info("Licences disponibles :");
            foreach (var (id, name, _) in Licenses)
                ConsoleService.Info($"  {id} — {name}");
            return 0;
        }

        var license = Licenses.FirstOrDefault(l => l.Id == type.ToLowerInvariant());
        if (license == default)
        {
            ConsoleService.Error($"Licence '{type}' inconnue. Choisis : {string.Join(", ", Licenses.Select(l => l.Id))}");
            return 1;
        }

        author ??= Environment.UserName;
        year ??= DateTime.Now.Year.ToString();
        var outputDir = output?.FullName ?? Directory.GetCurrentDirectory();
        Directory.CreateDirectory(outputDir);

        var content = license.Body
            .Replace("{year}", year)
            .Replace("{author}", author);

        var filePath = Path.Combine(outputDir, "LICENSE");
        File.WriteAllText(filePath, content);
        ConsoleService.Success($"Licence {license.Name} creee : {filePath}");
        return 0;
    }
}
