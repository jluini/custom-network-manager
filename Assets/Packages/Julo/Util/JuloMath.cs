
using System;

using UnityEngine;

namespace Julo.Util {

	public class JuloMath {
		public static float PI_180 = Mathf.PI / 180f;
		
		public static float abs(float x) {
			return x > 0f ? x : -x;
		}
		public static float minimax(float x, float minValue, float maxValue) {
			return Mathf.Min(Mathf.Max(x, minValue), maxValue);
		}
		public static float round(float value) {
			return Mathf.Round(value);
		}
		public static bool inRange(float x, float minValue, float maxValue) {
			return x >= minValue && x <= maxValue;
		}
		public static int sign(float x) {
			return x < 0 ? -1 : (x > 0 ? +1 : 0);
		}
		/**
		 * Returns a unit vector with the specified angle (in degrees).
		 */
		public static Vector2 unitVector(float degrees) {
			float r = radians(degrees);
			return new Vector2(Mathf.Cos(r), Mathf.Sin(r));
		}
		
		public static float radians(float degrees) {
			return degrees * PI_180;
		}
		public static float degrees(Vector2 vector) {
			if(vector.x == 0f) {
				return vector.y >= 0f ? 90f : -90f;
			} else {
				float ret = Mathf.Atan(vector.y / vector.x) / JuloMath.PI_180;
				return vector.x > 0f ? ret : ret + 180f;
			}
		}
		public static bool randomBool() {
			return randomBool(0.5f);
		}
		public static bool randomBool(float trueChance) {
			float rval = UnityEngine.Random.value;
			bool ret = rval < trueChance;
			return ret;
		}
		
		public static int randomInt(int min, int max) {
			float r = UnityEngine.Random.value * (max - min + 1);
			int rInt = (int)(Mathf.Floor(r));
			if(rInt >= max - min + 1) {
				Debug.Log("Random value was 1.0: " + r + " (" + rInt + ")");
				rInt = max - min;
			}
			return min + rInt;
		}
		
		public static float randomFloat(AnimationCurve curve, float mean, float deviation) {
			float d = curve.Evaluate(UnityEngine.Random.value) * 2f - 1f;
			return mean + d * deviation;
		}
		
		public static float randomFloat(float mean, float deviation) {
			return mean + UnityEngine.Random.Range(-deviation, +deviation);
		}
		
		public static float round(float number, int decimals = 0) {
			float power = Mathf.Pow(10f, decimals);
			return Mathf.Round(number * power) / power;
		}
		public static float floor(float number, int decimals = 0) {
			float power = Mathf.Pow(10f, decimals);
			return Mathf.Floor(number * power) / power;
		}
		public static float ceil(float number, int decimals = 0) {
			float power = Mathf.Pow(10f, decimals);
			return Mathf.Ceil(number * power) / power;
		}
		
		public enum TruncateMethod { ROUND, FLOOR, CEIL }
		
		public static float truncate(float number, TruncateMethod tm, int decimals = 0) {
			if(tm == TruncateMethod.ROUND) {
				return round(number, decimals);
			} else if(tm == TruncateMethod.FLOOR) {
				return floor(number, decimals);
			} else if(tm == TruncateMethod.CEIL) {
				return ceil(number, decimals);
			} else {
				throw new ApplicationException("Unknown TruncateMethod");
			}
		}
		
		public static string floatToString(float number, TruncateMethod tm, int decimals) {
			if(decimals < 0) {
				throw new ApplicationException("decimals cannot be negative");
			} else if(decimals == 0) {
				return truncate(number, tm).ToString();
			} else if(number == 0f) {
				char[] ret = new char[decimals + 2];
				ret[0] = '0';
				ret[1] = '.';
				for(int i = 0; i < decimals; i++) {
					ret[i + 2] = '0';
				}
				return new String(ret);
			} else if(number < 0f) {
				return "-" + floatToString(-number, tm, decimals); // TODO string joining
			} else {
				float power = Mathf.Pow(10f, decimals);
				
				float  poweredFloat = truncate(number * power, tm, 0);
				int    poweredInt   = (int)poweredFloat;
				
				string str = poweredInt.ToString();
				int strLen = str.Length;
				
				int retLen = Mathf.Max(strLen + 1, decimals + 2);
				char[] ret = new char[retLen];
				
				int dotIndex = retLen - 1 - decimals;
				
				for(int j = 0; j < retLen; j++) {
					char ch;
					if(j < dotIndex) {
						int index = strLen - retLen + j + 1;
						ch = index >= 0 ? str[index] : '0';
					} else if(j == dotIndex) {
						ch = '.';
					} else {
						int index = strLen - retLen + j;
						ch = index >= 0 ? str[index] : '0';
					}
					ret[j] = ch;
				}
				
				return new String(ret);
			}
		}
	}
}
