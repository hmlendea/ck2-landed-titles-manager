﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using NuciDAL.Repositories;
using NuciExtensions;

using CK2LandedTitlesManager.BusinessLogic.Mapping;
using CK2LandedTitlesManager.BusinessLogic.Models;
using CK2LandedTitlesManager.DataAccess.IO;
using CK2LandedTitlesManager.Models;

namespace CK2LandedTitlesManager.BusinessLogic
{
    public sealed class LandedTitleManager
    {
        List<LandedTitle> landedTitles;

        readonly INameValidator nameValidator;

        public LandedTitleManager()
        {
            landedTitles = new List<LandedTitle>();
            nameValidator = new NameValidator();
        }

        public LandedTitle Get(string id)
        {
            return landedTitles.FirstOrDefault(x => x.Id == id);
        }

        public IEnumerable<LandedTitle> GetAll()
        {
            return landedTitles;
        }

        public void LoadTitles(string fileName)
        {
            List<LandedTitle> loadedTitles = LoadTitlesFromFile(fileName).ToList();

            landedTitles.AddRange(loadedTitles);
            landedTitles = MergeTitles(landedTitles).ToList();
        }

        public void RemoveDynamicNamesFromFile(string fileName)
        {
            IEnumerable<LandedTitle> landedTitlesToRemove = LoadTitlesFromFile(fileName);

            MergeTitles(landedTitlesToRemove);
            RemoveDynamicNames(landedTitlesToRemove);
        }

        public void RemoveDynamicNames()
        {
            foreach (LandedTitle title in landedTitles)
            {
                title.DynamicNames.Clear();
            }
        }

        public bool CheckIntegrity(string fileName)
        {
            List<LandedTitle> masterTitles = LandedTitlesFile
                .ReadAllTitles(fileName)
                .ToDomainModels()
                .ToList();

            Dictionary<string, string> violations = new Dictionary<string, string>();

            foreach (LandedTitle title in landedTitles)
            {
                LandedTitle masterTitle = masterTitles.FirstOrDefault(x => x.Id == title.Id);

                if (landedTitles.Count(x => x.Id == title.Id) > 1)
                {
                    AddReasonToViolations(violations, title.Id, "Title is defined multiple times");
                    continue;
                }

                if (masterTitle == null)
                {
                    AddReasonToViolations(violations, title.Id, "Master file does not contain this title");
                    continue;
                }

                if (masterTitle.ParentId != title.ParentId)
                {
                    AddReasonToViolations(violations, title.Id, $"Master title has different parent ({title.ParentId} should be {masterTitle.ParentId})");
                    continue;
                }

                foreach (string cultureId in title.DynamicNames.Keys)
                {
                    if (!nameValidator.IsNameValid(title.DynamicNames[cultureId]))
                    {
                        AddReasonToViolations(violations, title.Id, $"Invalid name for {cultureId}");
                    }
                }
            }

            return violations.Count == 0;
        }

        public IDictionary<string, int> GetDynamicNamesCount()
        {
            IDictionary<string, int> dynamicNamesCount = new Dictionary<string, int>();

            foreach (LandedTitle title in landedTitles)
            {
                foreach (string cultureId in title.DynamicNames.Keys)
                {
                    if (!dynamicNamesCount.ContainsKey(cultureId))
                    {
                        dynamicNamesCount.Add(cultureId, 0);
                    }

                    dynamicNamesCount[cultureId] += 1;
                }
            }

            return dynamicNamesCount
                .OrderBy(x => x.Value)
                .Reverse()
                .ToDictionary(x => x.Key, x => x.Value);
        }

        public IEnumerable<OverwrittenDynamicName> GetOverwrittenDynamicNames(string fileName)
        {
            List<LandedTitle> masterTitles = LandedTitlesFile
                .ReadAllTitles(fileName)
                .ToDomainModels()
                .ToList();

            IList<OverwrittenDynamicName> overwrittenNames = new List<OverwrittenDynamicName>();

            foreach (LandedTitle title in landedTitles)
            {
                LandedTitle masterTitle = masterTitles.FirstOrDefault(x => x.Id == title.Id);

                if (masterTitle == null)
                {
                    continue;
                }

                foreach (string cultureId in title.DynamicNames.Keys)
                {
                    if (!masterTitle.DynamicNames.ContainsKey(cultureId) ||
                        masterTitle.DynamicNames[cultureId] == title.DynamicNames[cultureId])
                    {
                        continue;
                    }

                    OverwrittenDynamicName overwrittenName = new OverwrittenDynamicName();
                    overwrittenName.TitleId = title.Id;
                    overwrittenName.CultureId = cultureId;
                    overwrittenName.OriginalName = masterTitle.DynamicNames[cultureId];
                    overwrittenName.FinalName = title.DynamicNames[cultureId];

                    overwrittenNames.Add(overwrittenName);
                }
            }
            
            return overwrittenNames;
        }

        public void CleanFile(string fileName)
        {
            List<LandedTitle> oldLandedTitles=  landedTitles.ToList();
            landedTitles = new List<LandedTitle>();
            landedTitles = LoadTitlesFromFile(fileName).ToList();

            List<string> oldLines = File
                .ReadAllLines(fileName)
                .Distinct()
                .ToList();

            SaveTitles(fileName);

            List<string> newLines = File.ReadAllLines(fileName).ToList();

            Dictionary<string, string> dict = new Dictionary<string, string>();

            for (int i = 0; i < oldLines.Count; i++)
            {
                string mcnLine = oldLines[i];

                if (string.IsNullOrWhiteSpace(mcnLine) || !mcnLine.Contains('#'))
                {
                    continue;
                }

                string key = Regex
                    .Match(mcnLine, "^([^#]*)[\t ]*#.*$")
                    .Groups[1]
                    .Value
                    .TrimEnd();

                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(key) && !dict.ContainsKey(key))
                {
                    dict.Add(key, mcnLine);
                }
            }

            for (int i = 0; i < newLines.Count; i++)
            {
                string myLine = newLines[i];

                if (string.IsNullOrWhiteSpace(myLine))
                {
                    continue;
                }

                if (dict.ContainsKey(myLine))
                {
                    string myIndentation = Regex.Match(myLine, "( *)[^ ]*").Groups[1].Value;
                    myLine = myIndentation + dict[myLine].TrimStart();
                    newLines[i] = myLine;

                    continue;
                }
            }

            File.WriteAllLines(fileName, newLines);

            landedTitles = oldLandedTitles;
        }

        public void SaveTitles(string fileName)
        {
            LandedTitlesFile.WriteAllTitles(fileName, landedTitles.ToEntities());
            string content = File.ReadAllText(fileName);

            content = Regex.Replace(content, "\t", "    ");
            content = Regex.Replace(content, "= *(\r\n|\r|\n).*{", "={");
            content = Regex.Replace(content, "=", " = ");
            content = Regex.Replace(content, "\"(\r\n|\r|\n)( *[ekdcb]_)", "\"\n\n$2");

            File.WriteAllText(fileName, content);

            List<LandedTitle> oldLandedTitles=  landedTitles.ToList();
            landedTitles = new List<LandedTitle>();
            landedTitles = LoadTitlesFromFile(fileName).ToList();
        }

        // TODO: Better name
        // TODO: Proper return type
        public List<string> GetNamesOfTitlesWithAllCultures(List<string> cultureIds)
        {
            List<string> findings = new List<string>();

            int cultureColumnWidth = cultureIds.Max(x => x.Length);

            foreach (LandedTitle title in landedTitles)
            {
                if (cultureIds.All(title.DynamicNames.ContainsKey))
                {
                    string prefix = " ";

                    List<string> uniqueNames = title.DynamicNames
                        .Where(x => cultureIds.Contains(x.Key))
                        .Select(x => x.Value)
                        .Distinct()
                        .ToList();

                    if (uniqueNames.Count == 1)
                    {
                        prefix = "X";
                    }

                    foreach (string cultureId in cultureIds)
                    {
                        string finding = $"{prefix} {title.Id}\t{cultureId.PadRight(cultureColumnWidth + 1, ' ')}{title.DynamicNames[cultureId]}";
                        findings.Add(finding);
                    }
                }
            }

            return findings;
        }

        IEnumerable<LandedTitle> MergeTitles(IEnumerable<LandedTitle> landedTitles)
        {
            return landedTitles
                .GroupBy(o => o.Id)
                .Select(g => g.Skip(1)
                              .Aggregate(g.First(),
                                        (a, o) =>
                                        {
                                            a.DynamicNames = a.DynamicNames
                                                .Concat(o.DynamicNames)
                                                .GroupBy(e => e.Key)
                                                .ToDictionary(d => d.Key, d => d.First().Value);
                                            a.ReligiousValues = a.ReligiousValues
                                                .Concat(o.ReligiousValues)
                                                .GroupBy(e => e.Key)
                                                .ToDictionary(d => d.Key, d => d.First().Value);

                                            return a;
                                        }));
        }

        void RemoveDynamicNames(IEnumerable<LandedTitle> landedTitlesToRemove)
        {
            foreach (LandedTitle landedTitle in landedTitles)
            {
                LandedTitle landedTitleToRemove = landedTitlesToRemove.FirstOrDefault(x => x.Id == landedTitle.Id);

                if (landedTitleToRemove != null)
                {
                    List<string> cultureIds = landedTitle.DynamicNames.Keys.ToList();

                    foreach(string cultureId in cultureIds)
                    {
                        if (landedTitleToRemove.DynamicNames.ContainsKey(cultureId))
                        {
                            landedTitle.DynamicNames.Remove(cultureId);
                        }
                    }
                }
            }
        }

        void AddReasonToViolations(Dictionary<string, string> violations, string titleId, string reason)
        {
            Console.WriteLine(titleId + " " + reason);

            if (violations.ContainsKey(titleId))
            {
                violations[titleId] += $"; {reason}";
            }
            else
            {
                violations.Add(titleId, reason);
            }
        }

        IEnumerable<LandedTitle> LoadTitlesFromFile(string fileName)
        {
            IEnumerable<LandedTitle> titles = LandedTitlesFile
                .ReadAllTitles(fileName)
                .ToDomainModels();

            titles = MergeTitles(titles);

            return titles;
        }
    }
}
