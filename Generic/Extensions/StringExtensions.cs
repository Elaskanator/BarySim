﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic {
	public static class StringExtensions {
		public static readonly char[] Base16Chars = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

		public static string ToString_Number2(this double value, int desiredPrecision, bool includeTrailingZeros, int maxDigits = 7) {
			if (desiredPrecision < 1) throw new ArgumentOutOfRangeException("desiredPrecision");
			if (maxDigits < 1) throw new ArgumentOutOfRangeException("maxDigits");

			if (value == 0) {
				if (maxDigits == 1 || desiredPrecision < 2) return "0";
				else {
					int remainder = desiredPrecision - 1;
					remainder = remainder > desiredPrecision ? desiredPrecision : remainder;
					if (remainder == 0) return "0.";
					else return "0." + new string('0', remainder);
				}
			} else {
				int magnitude = (int)value.BaseExponent();
				int remainder = desiredPrecision - magnitude - 1;
				remainder = remainder > 0 ? remainder : 0;
				if (remainder > 0 && includeTrailingZeros)
					return value.ToString("N" + remainder);
				else return value.ToString("F0");
			}
		}
		public static string ToString_Number2(this double value, int desiredPrecision = 5, int maxDigits = 7) {
			return ToString_Number2(value, desiredPrecision, false, maxDigits);
		}
		public static string ToString_Number3(this double value, int totalLength = 4, bool includeTrailingZeros = false) {
			if (value == 0) {
				return "0";
			} else {
				double mag = value.BaseExponent();
				int magnitude = (int)mag;

				int remainingLen;
				if (includeTrailingZeros) {
					if (magnitude >= 0) {
						remainingLen = totalLength - magnitude - (value < 0 ? 3 : 2);
						if (remainingLen > 0)
							return value.ToString("N" + remainingLen);
						else return value.ToString("N0");
				
					} else {
						remainingLen = totalLength - (value < 0 ? 3 : 2);
						if (remainingLen >= 0)
							return value.ToString("N" + remainingLen);
						else return value.ToString("N0");
					}
				} else return ((int)value).ToString();
			}
		}

		public static string ToNumericString(this string noun, int number) {
			return string.Format("{0} {1}", number, noun.Pluralize(number));
		}
		public static string ToNumericString(this string noun, double number) {
			return string.Format("{0} {1}", number, noun.Pluralize(number));
		}

		public static string Pluralize(this string noun, int number) {
			if (number == 1) return noun;//why are values in the range [0, 1) made plural? fuck english, that's why
			else return noun.GetPluralForm();
		}
		public static string Pluralize(this string noun, double number) {
			if (number == 1) return noun;
			else return noun.GetPluralForm();
		}
		public static string Pluralize(this int number, string noun) {
			return string.Format("{0} {1}", number, noun.Pluralize(number));
		}
		public static string Pluralize(this double number, string noun) {
			return string.Format("{0} {1}", number, noun.Pluralize(number));
		}

		public static readonly char[] Vowels = new char[] { 'a', 'e', 'i', 'o', 'u' };
		//make sure to add move as they are encountered
		private static Lazy<HashSet<string>> _pluralRule_unchanged = new Lazy<HashSet<string>>(() => new HashSet<string>() {
			"sheep",
			"series",
			"species",
			"deer",
			"moose",
			"fish"//usually
			//{ "", "" },
		});
		private static Lazy<Dictionary<string, string>> _pluralRule_irregular = new Lazy<Dictionary<string, string>>(() => new Dictionary<string, string>() {
			{ "datum", "data" },
			{ "goose", "geese" },
			{ "child", "children" },
			{ "mouse", "mice" },
			{ "person", "people" },
			{ "louse", "lice" },
			{ "tooth", "teeth" },
			{ "foot", "feet" },
			//{ "", "" },
		});
		private static Lazy<HashSet<string>> _pluralRule_forceAddS = new Lazy<HashSet<string>>(() => new HashSet<string>() {
			"photo",
			"piano",
			"halo",
			"roof",
			"belief",
			"chef",
			"chief",
		});
		private static Lazy<HashSet<string>> _pluralRule_forceAddES = new Lazy<HashSet<string>>(() => new HashSet<string>() {
			"bus",
			"blitz",
		});
		private static Lazy<Dictionary<string, string>> _pluralRule_lastTwoReplacements = new Lazy<Dictionary<string, string>>(() => new Dictionary<string, string>() {
			{ "us", "i" },
			{ "fe", "ves" },
			{ "is", "es" },
			{ "on", "a" },
			{ "an", "en" }
		});
		public static string GetPluralForm(this string noun) {//fuck english
			if (noun == null || noun.Length == 0) return null;

			string lowercase = noun.ToLower();
			bool isLowercase = noun == lowercase;
			bool isUppercase = noun[0] != lowercase[0];

			bool isCaps = false;
			if (isUppercase) {
				isCaps = true;
				for (int i = 0; i < noun.Length; i++) {
					if (noun[i] == lowercase[i]) {
						isCaps = false;
						break;
					}
				}
			}

			string replacement;
			if (_pluralRule_unchanged.Value.Contains(lowercase)) {
				return noun;
			} else if (_pluralRule_forceAddS.Value.Contains(lowercase)) {
				return noun + (isCaps ? "S" : "s");
			} else if (_pluralRule_forceAddES.Value.Contains(lowercase)) {
				return noun + (isCaps ? "ES" : "es");
			} else if (_pluralRule_irregular.Value.TryGetValue(lowercase, out replacement)) {
				return isCaps
					? replacement.ToUpper()
					: isUppercase
						? replacement[0].ToString().ToUpper() + replacement.Substring(1)
						: replacement;
			} else if (_pluralRule_lastTwoReplacements.Value.TryGetValue(lowercase.Substring(noun.Length - 2), out replacement)) {
				return noun.Remove(noun.Length - 2)
					+ (isCaps ? replacement.ToUpper() : replacement);
			} else {
				char last = lowercase[^1];
				if (last == 'o' || last == 'x') {
					return noun + (isCaps ? "ES" : "es");
				} else if (last == 'f') {
					return noun.Remove(noun.Length - 1) + (isCaps ? "VES" : "ves");
				} else if (last == 'y' && !Vowels.Contains(lowercase[^2])) {
					return noun.Remove(noun.Length - 1)
						+ (isCaps ? "IES" : "ies");
				} else if (last == 's' || last == 'z') {//doubling last letter case
					string result = noun
						+ (lowercase[^2] == last
							? ""
							: lowercase[^1] == 's' ? "" : (isCaps ? last.ToString().ToUpper() : last.ToString()))
						+ (lowercase[^1] == 's' ? "" : (isCaps ? "ES" : "es"));
					return result;
				} else {//finally, the "normal" pluralization rule
					return noun + (isCaps ? "S" : "s");
					/*
					bool foundVowel = false;
					int i = 1;
					while (i < lowercase.Length - 1) {
						foundVowel = Vowels.Contains(lowercase[^i]);
						if (foundVowel) break;
						else i++;
					}
					if (foundVowel && i > 1 && lowercase[^i] == 'o' && lowercase[^(i + 1)] == 'o') {//replace ooxyz with eexyz, e.g. tooth > teeth
						return noun.Substring(0, noun.Length - 1 - i)
							+ (isCaps ? "EE" : "ee")
							+ noun.Substring(noun.Length + 1 - i);
					} else {//finally, the "normal" pluralization rule
					}
					*/
				}
			}
		}

		public static string ToString_Base(this int value, int b) {
			if (b < 2 || b > 16) throw new Exception();
			
			string result = string.Empty;
			while (value > 0) {
				result = Base16Chars[value % b] + result;
				value /= b;
			}
			return result;
		}

		public static string PadCenter(this string str, int len) {
			int lengthDiff = len - str.Length;
			if (lengthDiff > 0) {
				int
					left = lengthDiff / 2,
					right = len - left - str.Length;
				return string.Format("{0}{1}{2}",
					new string(' ', left),
					str,
					new string(' ', right));
			} else return str;
		}
	}
}