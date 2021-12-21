using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace ConsoleApp1 {
	class Program {
		static void Main(string[] args) {
			int mask = 0b10011010;// 154

			Vector<int> maskVector = new Vector<int>(
				Enumerable.Range(0, Vector<int>.Count)
					.Select(i => (mask & (1 << i)) > 0 ? -1 : 0)
					.ToArray());
			string maskVectorStr = string.Join("", maskVector);//wonky behavior

			//<0, 1, 0, 1, 1, 0, 0, 1>
			Vector<int> ifTrueVector = new Vector<int>(Enumerable.Range(0, Vector<int>.Count).Select(i => 1 << i).ToArray());
			//powers of two <1, 2, 4, 8, 16, 32, 64, 128>
			Vector<int> ifFalseVector = Vector<int>.Zero;

			Vector<int> resultVector = Vector.ConditionalSelect(maskVector, ifTrueVector, ifFalseVector);
			string resultStr = string.Join("", resultVector);

			// our original mask value back
			int sum = Vector.Dot(resultVector, Vector<int>.One);
		}
	}
}
