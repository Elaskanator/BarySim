using System;
using System.Collections.Generic;
using System.Linq;

namespace _Throwaway {
	class Program {
		static void Main(string[] args) {
			IEnumerable<BaseClass> derivedType = new BaseClass[] { new DerivedClass(), new DerivedClass() };
			DerivedClass[] parentType = derivedType.Cast<DerivedClass>().ToArray();
		}

		class BaseClass { }
		class DerivedClass : BaseClass { }
	}
}
