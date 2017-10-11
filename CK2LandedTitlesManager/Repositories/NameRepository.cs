using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

using CK2LandedTitlesManager.Models;

namespace CK2LandedTitlesManager.Repositories
{
    public class NameRepository : Repository<Name>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NameRepository"/> class.
        /// </summary>
        public NameRepository()
        {
        }

        /// <summary>
        /// Gets all names by the title identifier.
        /// </summary>
        /// <returns>List of names.</returns>
        /// <param name="titleId">The title identifier</param>
        public IEnumerable<Name> GetAllByTitleId(int titleId)
        {
            IEnumerable<Name> names = GetAll();

            return names.Where(x => x.TitleId == titleId);
        }

        /// <summary>
        /// Gets all names by name.
        /// </summary>
        /// <returns>List of names.</returns>
        /// <param name="text">The name</param>
        public IEnumerable<Name> GetAllByText(string text)
        {
            IEnumerable<Name> names = GetAll();

            return names.Where(x => x.Text == text);
        }

        /// <summary>
        /// Loads the names
        /// </summary>
        /// <param name="fileName">Path to the input file</param>
        public void Load(string fileName)
        {
            List<string> lines = File.ReadAllLines(Path.GetFullPath(fileName)).ToList();
            List<string> blacklistPatterns = File.ReadAllLines("non_cultures.lst").ToList();
            Regex regex = new Regex(@"^([a-z]* = " + "\"" + @"*\p{L}+( \p{L}+)*" + "\"*)$");

            for (int i = 1; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                Match match = regex.Match(line);

                if (match.Success)
                {
                    string[] split = line.Split('=');
                    string culture = split[0].Trim();
                    string text = split[1].Trim().Replace("\"", "").Replace("_", " ");
                    bool valid = true;

                    foreach (string pattern in blacklistPatterns)
                    {
                        Regex blacklistRegex = new Regex(pattern);

                        if (blacklistRegex.IsMatch(culture))
                        {
                            valid = false;
                            break;
                        }
                    }

                    if (valid)
                    {
                        Name name = new Name
                        {
                            Id = i,
                            Culture = culture,
                            Text = text
                        };
                        
                        Add(name);
                    }
                }
            }
        }
    }
}
