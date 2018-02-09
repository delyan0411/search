/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Laobai.Lucene.Search;
using NumericUtils = Laobai.Lucene.Util.NumericUtils;

namespace Laobai.Lucene.Documents
{
	
	/// <summary> Provides support for converting dates to strings and vice-versa.
	/// The strings are structured so that lexicographic sorting orders 
	/// them by date, which makes them suitable for use as field values 
	/// and search terms.
	/// 
	/// <p/>This class also helps you to limit the resolution of your dates. Do not
	/// save dates with a finer resolution than you really need, as then
	/// RangeQuery and PrefixQuery will require more memory and become slower.
	/// 
	/// <p/>Compared to <see cref="DateField" /> the strings generated by the methods
	/// in this class take slightly more space, unless your selected resolution
	/// is set to <c>Resolution.DAY</c> or lower.
	/// 
	/// <p/>
	/// Another approach is <see cref="NumericUtils" />, which provides
	/// a sortable binary representation (prefix encoded) of numeric values, which
	/// date/time are.
    /// For indexing a <see cref="DateTime" />, convert it to unix timestamp as
	/// <c>long</c> and
	/// index this as a numeric value with <see cref="NumericField" />
	/// and use <see cref="NumericRangeQuery{T}" /> to query it.
	/// </summary>
	public class DateTools
	{
		
        private static readonly System.String YEAR_FORMAT = "yyyy";
        private static readonly System.String MONTH_FORMAT = "yyyyMM";
        private static readonly System.String DAY_FORMAT = "yyyyMMdd";
        private static readonly System.String HOUR_FORMAT = "yyyyMMddHH";
        private static readonly System.String MINUTE_FORMAT = "yyyyMMddHHmm";
        private static readonly System.String SECOND_FORMAT = "yyyyMMddHHmmss";
        private static readonly System.String MILLISECOND_FORMAT = "yyyyMMddHHmmssfff";
		
		private static readonly System.Globalization.Calendar calInstance = new System.Globalization.GregorianCalendar();
		
		// cannot create, the class has static methods only
		private DateTools()
		{
		}
		
		/// <summary> Converts a Date to a string suitable for indexing.
		/// 
		/// </summary>
		/// <param name="date">the date to be converted
		/// </param>
		/// <param name="resolution">the desired resolution, see
		/// <see cref="Round(DateTime, DateTools.Resolution)" />
		/// </param>
		/// <returns> a string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
		/// depending on <c>resolution</c>; using GMT as timezone 
		/// </returns>
		public static System.String DateToString(System.DateTime date, Resolution resolution)
		{
			return TimeToString(date.Ticks / TimeSpan.TicksPerMillisecond, resolution);
		}
		
		/// <summary> Converts a millisecond time to a string suitable for indexing.
		/// 
		/// </summary>
		/// <param name="time">the date expressed as milliseconds since January 1, 1970, 00:00:00 GMT
		/// </param>
		/// <param name="resolution">the desired resolution, see
		/// <see cref="Round(long, DateTools.Resolution)" />
		/// </param>
		/// <returns> a string in format <c>yyyyMMddHHmmssSSS</c> or shorter,
		/// depending on <c>resolution</c>; using GMT as timezone
		/// </returns>
		public static System.String TimeToString(long time, Resolution resolution)
		{
            System.DateTime date = new System.DateTime(Round(time, resolution));
			
			if (resolution == Resolution.YEAR)
			{
                return date.ToString(YEAR_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (resolution == Resolution.MONTH)
			{
                return date.ToString(MONTH_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (resolution == Resolution.DAY)
			{
                return date.ToString(DAY_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (resolution == Resolution.HOUR)
			{
                return date.ToString(HOUR_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (resolution == Resolution.MINUTE)
			{
                return date.ToString(MINUTE_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (resolution == Resolution.SECOND)
			{
                return date.ToString(SECOND_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (resolution == Resolution.MILLISECOND)
			{
                return date.ToString(MILLISECOND_FORMAT, System.Globalization.CultureInfo.InvariantCulture);
			}
			
			throw new System.ArgumentException("unknown resolution " + resolution);
		}
		
		/// <summary> Converts a string produced by <c>timeToString</c> or
		/// <c>DateToString</c> back to a time, represented as the
		/// number of milliseconds since January 1, 1970, 00:00:00 GMT.
		/// 
		/// </summary>
		/// <param name="dateString">the date string to be converted
		/// </param>
		/// <returns> the number of milliseconds since January 1, 1970, 00:00:00 GMT
		/// </returns>
		/// <throws>  ParseException if <c>dateString</c> is not in the  </throws>
		/// <summary>  expected format 
		/// </summary>
		public static long StringToTime(System.String dateString)
		{
			return StringToDate(dateString).Ticks;
		}
		
		/// <summary> Converts a string produced by <c>timeToString</c> or
		/// <c>DateToString</c> back to a time, represented as a
		/// Date object.
		/// 
		/// </summary>
		/// <param name="dateString">the date string to be converted
		/// </param>
		/// <returns> the parsed time as a Date object 
		/// </returns>
		/// <throws>  ParseException if <c>dateString</c> is not in the  </throws>
		/// <summary>  expected format 
		/// </summary>
		public static System.DateTime StringToDate(System.String dateString)
		{
            System.DateTime date;
            if (dateString.Length == 4)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    1, 1, 0, 0, 0, 0);
            }
            else if (dateString.Length == 6)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    Convert.ToInt16(dateString.Substring(4, 2)),
                    1, 0, 0, 0, 0);
            }
            else if (dateString.Length == 8)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    Convert.ToInt16(dateString.Substring(4, 2)),
                    Convert.ToInt16(dateString.Substring(6, 2)),
                    0, 0, 0, 0);
            }
            else if (dateString.Length == 10)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    Convert.ToInt16(dateString.Substring(4, 2)),
                    Convert.ToInt16(dateString.Substring(6, 2)),
                    Convert.ToInt16(dateString.Substring(8, 2)),
                    0, 0, 0);
            }
            else if (dateString.Length == 12)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    Convert.ToInt16(dateString.Substring(4, 2)),
                    Convert.ToInt16(dateString.Substring(6, 2)),
                    Convert.ToInt16(dateString.Substring(8, 2)),
                    Convert.ToInt16(dateString.Substring(10, 2)),
                    0, 0);
            }
            else if (dateString.Length == 14)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    Convert.ToInt16(dateString.Substring(4, 2)),
                    Convert.ToInt16(dateString.Substring(6, 2)),
                    Convert.ToInt16(dateString.Substring(8, 2)),
                    Convert.ToInt16(dateString.Substring(10, 2)),
                    Convert.ToInt16(dateString.Substring(12, 2)),
                    0);
            }
            else if (dateString.Length == 17)
            {
                date = new System.DateTime(Convert.ToInt16(dateString.Substring(0, 4)),
                    Convert.ToInt16(dateString.Substring(4, 2)),
                    Convert.ToInt16(dateString.Substring(6, 2)),
                    Convert.ToInt16(dateString.Substring(8, 2)),
                    Convert.ToInt16(dateString.Substring(10, 2)),
                    Convert.ToInt16(dateString.Substring(12, 2)),
                    Convert.ToInt16(dateString.Substring(14, 3)));
            }
            else
            {
                throw new System.FormatException("Input is not valid date string: " + dateString);
            }
            return date;
		}

	    /// <summary> Limit a date's resolution. For example, the date <c>2004-09-21 13:50:11</c>
	    /// will be changed to <c>2004-09-01 00:00:00</c> when using
	    /// <c>Resolution.MONTH</c>. 
	    /// 
	    /// </summary>
	    /// <param name="date"></param>
	    /// <param name="resolution">The desired resolution of the date to be returned
	    /// </param>
	    /// <returns> the date with all values more precise than <c>resolution</c>
	    /// set to 0 or 1
	    /// </returns>
	    public static System.DateTime Round(System.DateTime date, Resolution resolution)
		{
			return new System.DateTime(Round(date.Ticks / TimeSpan.TicksPerMillisecond, resolution));
		}
		
		/// <summary> Limit a date's resolution. For example, the date <c>1095767411000</c>
		/// (which represents 2004-09-21 13:50:11) will be changed to 
		/// <c>1093989600000</c> (2004-09-01 00:00:00) when using
		/// <c>Resolution.MONTH</c>.
		/// 
		/// </summary>
		/// <param name="time">The time in milliseconds (not ticks).</param>
		/// <param name="resolution">The desired resolution of the date to be returned
		/// </param>
		/// <returns> the date with all values more precise than <c>resolution</c>
		/// set to 0 or 1, expressed as milliseconds since January 1, 1970, 00:00:00 GMT
		/// </returns>
		public static long Round(long time, Resolution resolution)
		{
			System.DateTime dt = new System.DateTime(time * TimeSpan.TicksPerMillisecond);
			
			if (resolution == Resolution.YEAR)
			{
                dt = dt.AddMonths(1 - dt.Month);
                dt = dt.AddDays(1 - dt.Day);
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
			else if (resolution == Resolution.MONTH)
			{
                dt = dt.AddDays(1 - dt.Day);
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
			else if (resolution == Resolution.DAY)
			{
                dt = dt.AddHours(0 - dt.Hour);
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
			else if (resolution == Resolution.HOUR)
			{
                dt = dt.AddMinutes(0 - dt.Minute);
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
			else if (resolution == Resolution.MINUTE)
			{
                dt = dt.AddSeconds(0 - dt.Second);
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
			else if (resolution == Resolution.SECOND)
			{
                dt = dt.AddMilliseconds(0 - dt.Millisecond);
            }
			else if (resolution == Resolution.MILLISECOND)
			{
				// don't cut off anything
			}
			else
			{
				throw new System.ArgumentException("unknown resolution " + resolution);
			}
			return dt.Ticks;
		}
		
		/// <summary>Specifies the time granularity. </summary>
		public class Resolution
		{
			
			public static readonly Resolution YEAR = new Resolution("year");
			public static readonly Resolution MONTH = new Resolution("month");
			public static readonly Resolution DAY = new Resolution("day");
			public static readonly Resolution HOUR = new Resolution("hour");
			public static readonly Resolution MINUTE = new Resolution("minute");
			public static readonly Resolution SECOND = new Resolution("second");
			public static readonly Resolution MILLISECOND = new Resolution("millisecond");
			
			private System.String resolution;
			
			internal Resolution()
			{
			}
			
			internal Resolution(System.String resolution)
			{
				this.resolution = resolution;
			}
			
			public override System.String ToString()
			{
				return resolution;
			}
		}
		static DateTools()
		{
			{
				// times need to be normalized so the value doesn't depend on the 
				// location the index is created/used:
                // {{Aroush-2.1}}
                /*
				YEAR_FORMAT.setTimeZone(GMT);
				MONTH_FORMAT.setTimeZone(GMT);
				DAY_FORMAT.setTimeZone(GMT);
				HOUR_FORMAT.setTimeZone(GMT);
				MINUTE_FORMAT.setTimeZone(GMT);
				SECOND_FORMAT.setTimeZone(GMT);
				MILLISECOND_FORMAT.setTimeZone(GMT);
                */
			}
		}
	}
}