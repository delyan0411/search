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

namespace Laobai.Lucene.Search
{
	
	/// <summary> The Scorer for DisjunctionMaxQuery's.  The union of all documents generated by the the subquery scorers
	/// is generated in document number order.  The score for each document is the maximum of the scores computed
	/// by the subquery scorers that generate that document, plus tieBreakerMultiplier times the sum of the scores
	/// for the other subqueries that generate the document.
	/// </summary>
	class DisjunctionMaxScorer:Scorer
	{
		
		/* The scorers for subqueries that have remaining docs, kept as a min heap by number of next doc. */
		private Scorer[] subScorers;
		private int numScorers;
		/* Multiplier applied to non-maximum-scoring subqueries for a document as they are summed into the result. */
		private float tieBreakerMultiplier;
		private int doc = - 1;
		
		/// <summary> Creates a new instance of DisjunctionMaxScorer
		/// 
		/// </summary>
		/// <param name="tieBreakerMultiplier">Multiplier applied to non-maximum-scoring subqueries for a
		/// document as they are summed into the result.
		/// </param>
		/// <param name="similarity">-- not used since our definition involves neither coord nor terms
		/// directly
		/// </param>
		/// <param name="subScorers">The sub scorers this Scorer should iterate on
		/// </param>
		/// <param name="numScorers">The actual number of scorers to iterate on. Note that the array's
		/// length may be larger than the actual number of scorers.
		/// </param>
		public DisjunctionMaxScorer(float tieBreakerMultiplier, Similarity similarity, Scorer[] subScorers, int numScorers):base(similarity)
		{
			
			this.tieBreakerMultiplier = tieBreakerMultiplier;
			// The passed subScorers array includes only scorers which have documents
			// (DisjunctionMaxQuery takes care of that), and their nextDoc() was already
			// called.
			this.subScorers = subScorers;
			this.numScorers = numScorers;
			
			Heapify();
		}
		
		public override int NextDoc()
		{
			if (numScorers == 0)
				return doc = NO_MORE_DOCS;
			while (subScorers[0].DocID() == doc)
			{
				if (subScorers[0].NextDoc() != NO_MORE_DOCS)
				{
					HeapAdjust(0);
				}
				else
				{
					HeapRemoveRoot();
					if (numScorers == 0)
					{
						return doc = NO_MORE_DOCS;
					}
				}
			}
			
			return doc = subScorers[0].DocID();
		}
		
		public override int DocID()
		{
			return doc;
		}
		
		/// <summary>Determine the current document score.  Initially invalid, until <see cref="NextDoc()" /> is called the first time.</summary>
		/// <returns> the score of the current generated document
		/// </returns>
		public override float Score()
		{
			int doc = subScorers[0].DocID();
			float[] sum = new float[]{subScorers[0].Score()}, max = new float[]{sum[0]};
			int size = numScorers;
			ScoreAll(1, size, doc, sum, max);
			ScoreAll(2, size, doc, sum, max);
			return max[0] + (sum[0] - max[0]) * tieBreakerMultiplier;
		}
		
		// Recursively iterate all subScorers that generated last doc computing sum and max
		private void  ScoreAll(int root, int size, int doc, float[] sum, float[] max)
		{
			if (root < size && subScorers[root].DocID() == doc)
			{
				float sub = subScorers[root].Score();
				sum[0] += sub;
				max[0] = System.Math.Max(max[0], sub);
				ScoreAll((root << 1) + 1, size, doc, sum, max);
				ScoreAll((root << 1) + 2, size, doc, sum, max);
			}
		}
		
		public override int Advance(int target)
		{
			if (numScorers == 0)
				return doc = NO_MORE_DOCS;
			while (subScorers[0].DocID() < target)
			{
				if (subScorers[0].Advance(target) != NO_MORE_DOCS)
				{
					HeapAdjust(0);
				}
				else
				{
					HeapRemoveRoot();
					if (numScorers == 0)
					{
						return doc = NO_MORE_DOCS;
					}
				}
			}
			return doc = subScorers[0].DocID();
		}
		
		// Organize subScorers into a min heap with scorers generating the earliest document on top.
		private void  Heapify()
		{
			for (int i = (numScorers >> 1) - 1; i >= 0; i--)
			{
				HeapAdjust(i);
			}
		}
		
		/* The subtree of subScorers at root is a min heap except possibly for its root element.
		* Bubble the root down as required to make the subtree a heap.
		*/
		private void  HeapAdjust(int root)
		{
			Scorer scorer = subScorers[root];
			int doc = scorer.DocID();
			int i = root;
			while (i <= (numScorers >> 1) - 1)
			{
				int lchild = (i << 1) + 1;
				Scorer lscorer = subScorers[lchild];
				int ldoc = lscorer.DocID();
				int rdoc = System.Int32.MaxValue, rchild = (i << 1) + 2;
				Scorer rscorer = null;
				if (rchild < numScorers)
				{
					rscorer = subScorers[rchild];
					rdoc = rscorer.DocID();
				}
				if (ldoc < doc)
				{
					if (rdoc < ldoc)
					{
						subScorers[i] = rscorer;
						subScorers[rchild] = scorer;
						i = rchild;
					}
					else
					{
						subScorers[i] = lscorer;
						subScorers[lchild] = scorer;
						i = lchild;
					}
				}
				else if (rdoc < doc)
				{
					subScorers[i] = rscorer;
					subScorers[rchild] = scorer;
					i = rchild;
				}
				else
				{
					return ;
				}
			}
		}
		
		// Remove the root Scorer from subScorers and re-establish it as a heap
		private void  HeapRemoveRoot()
		{
			if (numScorers == 1)
			{
				subScorers[0] = null;
				numScorers = 0;
			}
			else
			{
				subScorers[0] = subScorers[numScorers - 1];
				subScorers[numScorers - 1] = null;
				--numScorers;
				HeapAdjust(0);
			}
		}
	}
}