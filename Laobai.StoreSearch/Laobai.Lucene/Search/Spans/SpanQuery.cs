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

using IndexReader = Laobai.Lucene.Index.IndexReader;
using Query = Laobai.Lucene.Search.Query;
using Searcher = Laobai.Lucene.Search.Searcher;
using Weight = Laobai.Lucene.Search.Weight;

namespace Laobai.Lucene.Search.Spans
{
	
	/// <summary>Base class for span-based queries. </summary>
	[Serializable]
	public abstract class SpanQuery:Query
	{
		/// <summary>Expert: Returns the matches for this query in an index.  Used internally
		/// to search for spans. 
		/// </summary>
		public abstract Spans GetSpans(IndexReader reader);

	    /// <summary>Returns the name of the field matched by this query.</summary>
	    public abstract string Field { get; }

	    public override Weight CreateWeight(Searcher searcher)
		{
			return new SpanWeight(this, searcher);
		}
	}
}