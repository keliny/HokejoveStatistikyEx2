using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("wrong arguments amount");
                Environment.Exit(0);
            }

            var dataFileName = args[0];
            var commandsFileName = args[1];
            var outputFileName = args[2];

            var filesOk = true;
            if (!File.Exists(dataFileName))
            {
                filesOk = false;
                Console.WriteLine("Soubor se vstupnimy daty neexistuje.");
            }

            if (!File.Exists(commandsFileName))
            {
                filesOk = false;
                Console.WriteLine("Soubor se vstupnimy daty neexistuje.");
            }

            if (!filesOk)
            {
                Environment.Exit(0);
            }

            // 
            var roundsToBeProcessed = new List<int>();
            var commandsXml = XElement.Load(commandsFileName);
            roundsToBeProcessed.AddRange(commandsXml.Element("rounds").Elements("round")
                .Select(elRound => int.Parse(elRound.Value)));

            foreach (var rangeEl in commandsXml.Element("rounds").Elements("range"))
            {
                int[] range = rangeEl.Descendants().Select(el => int.Parse(el.Value)).ToArray();
                roundsToBeProcessed.AddRange(Enumerable.Range(range[0], range[1] - range[0] + 1));
            }


            var teamStats = new Dictionary<string, TeamStats>();
            using (StreamReader sr = new StreamReader(dataFileName))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var lineValues = line.Split(',');
                    var round = int.Parse(lineValues[1]);
                    if (!roundsToBeProcessed.Contains(round))
                    {
                        continue;
                    }
                    Console.WriteLine($"processing round: {lineValues[1]}");


                    var homeTeamName = lineValues[2];
                    if (!teamStats.ContainsKey(homeTeamName))
                    {
                        teamStats.Add(homeTeamName, new TeamStats() { Name = homeTeamName });
                    }

                    TeamStats homeTeamStats = teamStats[homeTeamName];
                    Console.WriteLine($"home team before is: {homeTeamStats.Name} : score away = {homeTeamStats.ScoreAway} : score home = {homeTeamStats.ScoreHome}");

                    var awayTeamName = lineValues[3];
                    if (!teamStats.ContainsKey(awayTeamName))
                    {
                        teamStats.Add(awayTeamName, new TeamStats() { Name = awayTeamName });
                    }

                    TeamStats awayTeamStats = teamStats[awayTeamName];
                    Console.WriteLine($"away team before is: {awayTeamStats.Name} : score away = {awayTeamStats.ScoreAway} : score home = {awayTeamStats.ScoreHome}");


                    bool standardEnd = !lineValues[4].EndsWith("(PP)") && !lineValues[4].EndsWith("(SN)");
                    if (!standardEnd)
                    {
                        lineValues[4] = lineValues[4].Substring(0, lineValues[4].Length - 4);
                    }

                    int[] score = lineValues[4].Split(':').Select(s => int.Parse(s)).ToArray();

                    homeTeamStats.AttackHome += score[0];
                    homeTeamStats.DefenseHome += score[1];
                    awayTeamStats.AttackAway += score[1];
                    awayTeamStats.DefenseAway += score[0];

                    if (score[0] > score[1])
                    {
                        if (standardEnd)
                        {
                            homeTeamStats.ScoreHome += 3;
                        }
                        else
                        {
                            homeTeamStats.ScoreHome += 2;
                            awayTeamStats.ScoreAway += 1;
                        }
                    }
                    else
                    {
                        if (standardEnd)
                        {
                            awayTeamStats.ScoreAway += 3;
                        }
                        else
                        {
                            awayTeamStats.ScoreAway += 2;
                            homeTeamStats.ScoreHome += 1;
                        }
                    }
                    Console.WriteLine($"home team after is: {homeTeamStats.Name} : score away = {homeTeamStats.ScoreAway} : score home = {homeTeamStats.ScoreHome}");
                    Console.WriteLine($"away team before is: {awayTeamStats.Name} : score away = {awayTeamStats.ScoreAway} : score home = {awayTeamStats.ScoreHome}");

                }
            }

            using (var sw = new StreamWriter(outputFileName))
            {
                foreach (var cmdEl in commandsXml.Element("statistics").Elements())
                {
                    var numberOfTeams = int.Parse(cmdEl.Attribute("num").Value);
                    var type = cmdEl.Attribute("type").Value;
                    var cmdElName = cmdEl.Name.ToString();

                    bool sortDescending = cmdElName == "most-points" || cmdElName == "best-attack" ||
                                          cmdElName == "worst-defense";
                    IEnumerable<Tuple<string, int>> sortedTeamStats = null;
                    if (cmdElName.EndsWith("points"))
                    {
                        sw.Write(sortDescending ? "Nejlepsi tym" : "Nejhorsi tym");
                        if (type == "away")
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.ScoreAway)
                                    : teamStats.Values.OrderBy(s => s.ScoreAway))
                                .Select(s => new Tuple<string, int>(s.Name, s.ScoreAway));
                        }
                        else if (type == "home")
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.ScoreHome)
                                    : teamStats.Values.OrderBy(s => s.ScoreHome))
                                .Select(s => new Tuple<string, int>(s.Name, s.ScoreHome));
                        }
                        else
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.ScoreAway + s.ScoreAway)
                                    : teamStats.Values.OrderBy(s => s.ScoreAway + s.ScoreAway))
                                .Select(s => new Tuple<string, int>(s.Name, s.ScoreAway + s.ScoreAway));
                        }
                    }
                    else if (cmdElName.EndsWith("attack"))
                    {
                        sw.Write(sortDescending ? "Nejlepsi utok" : "Nejhorsi utok");
                        if (type == "away")
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.AttackAway)
                                    : teamStats.Values.OrderBy(s => s.AttackAway))
                                .Select(s => new Tuple<string, int>(s.Name, s.AttackAway));
                        }
                        else if (type == "home")
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.AttackHome)
                                    : teamStats.Values.OrderBy(s => s.AttackHome))
                                .Select(s => new Tuple<string, int>(s.Name, s.AttackHome));
                        }
                        else
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.AttackAway + s.AttackHome)
                                    : teamStats.Values.OrderBy(s => s.AttackAway + s.AttackHome))
                                .Select(s => new Tuple<string, int>(s.Name, s.AttackAway + s.AttackHome));
                        }
                    }
                    else
                    {
                        sw.Write(sortDescending ? "Nejhorsi obrana" : "Nejlepsi obrana");
                        if (type == "away")
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.DefenseAway)
                                    : teamStats.Values.OrderBy(s => s.DefenseAway))
                                .Select(s => new Tuple<string, int>(s.Name, s.DefenseAway));
                        }
                        else if (type == "home")
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.DefenseHome)
                                    : teamStats.Values.OrderBy(s => s.DefenseHome))
                                .Select(s => new Tuple<string, int>(s.Name, s.DefenseHome));
                        }
                        else
                        {
                            sortedTeamStats =
                                (sortDescending
                                    ? teamStats.Values.OrderByDescending(s => s.DefenseAway + s.DefenseHome)
                                    : teamStats.Values.OrderBy(s => s.DefenseAway + s.DefenseHome))
                                .Select(s => new Tuple<string, int>(s.Name, s.DefenseAway + s.DefenseHome));
                        }
                    }

                    switch (type)
                    {
                        case "away":
                            sw.WriteLine("(venku)");
                            break;
                        case "home":
                            sw.WriteLine("(doma)");
                            break;
                        default:
                            sw.WriteLine("(vsechno)");
                            break;
                    }

                    var counter = 1;

                    foreach (var stat in sortedTeamStats.Take(numberOfTeams))
                    {
                        sw.WriteLine($"{counter}, {stat.Item1} ({stat.Item2})");
                    }

                }
            }
        }
    }

    class TeamStats
    {
        public string Name { get; set; }
        public int ScoreAway { get; set; }
        public int ScoreHome { get; set; }
        public int DefenseAway { get; set; }
        public int DefenseHome { get; set; }
        public int AttackAway { get; set; }
        public int AttackHome { get; set; }
    }
}