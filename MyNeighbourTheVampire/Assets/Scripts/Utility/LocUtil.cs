using I2.Loc;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class LocUtil
{
	static public string Format(params object[] args)
	{
		//this is very fancy so I tried to make it as lean as possible
		string[] parts = null;
		for (var i = 0; i < args.Length; i++)
		{
			var o = args[i];
			if (o is string)
			{
				string key = null;
				if (i == 0)
				{
					key = o as string;
				}
				else
				{
					if (parts != null) parts = new string[8];
					parts[0] = args[0] as string;
					parts[1] = args[1] as string;
					key = string.Join(",", parts, 0, i);
				}
				var loc = _translateWithDefault(key);
				if (loc == "") continue;

				var nfargs = args.Length - i - 1;
				var fargs = new object[nfargs];
				for (var fnarg = 0; fnarg < nfargs; fnarg++)
					fargs[fnarg] = args[fnarg + i + 1];
				return string.Format(loc, fargs);
			}
		}

		return "";
	}

	static public string TranslateWithDefault(string def, params string[] parts)
	{
		if (parts.Length == 1)
			return _translateWithDefault(parts[0], def);
		else return _translateWithDefault(string.Join("/", parts), def: def);
	}

	static public string TranslateWithDefault(string def, bool logMissing, params string[] parts)
	{
		if (parts.Length == 1)
			return _translateWithDefault(parts[0], def: def, logMissing: logMissing);
		else return _translateWithDefault(string.Join("/", parts), def: def, logMissing: logMissing);
	}

	static public string TranslateWithDefault(string def, string key, bool logMissing, params string[] parts)
	{
		if (parts.Length == 1)
			return _translateWithDefault(parts[0], key, def, logMissing);
		else return _translateWithDefault(string.Join("/", parts), key, def, logMissing);
	}

	public static string Translate(params string[] parts)
	{
		if (parts.Length == 0)
			return "";

		if (parts.Length == 1)
			return _translateWithDefault(parts[0]);

		return _translateWithDefault(string.Join("/", parts));
	}

	static HashSet<string> missing = new HashSet<string>();

	static string _translateWithDefault(string term, string key = "dialogue", string def = null, bool logMissing = true)
	{
		var translation = LocalizationManager.GetTermTranslation(term, overrideLanguage: key);

		if (string.IsNullOrEmpty(translation)) //at some point i2loc changed from "" to null--lets check for both
		{
			if (logMissing && !missing.Contains(term))
			{
				if (Application.isPlaying)
					Debug.LogWarning("No localization term exists for '" + term + "'");

				missing.Add(term);
			}

			translation = def ?? term;
		}

		return Escape(translation);
	}

	// If we end up needing to change more characters it is better to reuse a static StringBuilder
	// and iterate on all characters replacing them when needed.
	private static string Escape(string translation)
	{
		if (string.IsNullOrEmpty(translation)) return "";
		return translation.Replace('“', '"').Replace('”', '"');
	}

	public static string PickTranslation(string category)
	{
		List<string> terms = LocalizationManager.GetTermsList(category);
		if (terms == null || terms.Count == 0) return Translate(category);
		var index = Random.Range(0, terms.Count);
		return Translate(terms[index]);
	}
}
