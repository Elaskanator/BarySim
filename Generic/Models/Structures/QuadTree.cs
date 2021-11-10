using System;
using System.Collections.Generic;
using System.Linq;

namespace Generic.Models {
	public abstract class AQuadTree<E, T> : ATree<E>
	where E : IVector<double>
	where T : AQuadTree<E, T> {
		public const int CAPACITY = 5;
		//dividing by 2 enough times WILL reach the sig figs limit of the underlying type and cause zero-sized subtrees (before reaching the stack frame depth limit due to recursion)
		public abstract int MaxDepth { get; }
		
		public readonly VectorDouble LeftCorner;
		public readonly VectorDouble Center;
		public readonly VectorDouble RightCorner;
		
		public int NumMembers { get; private set; }
		public int Dimensionality { get { return this.LeftCorner.Length; } }
		public override IEnumerable<T> Children { get { return this._quadrants; } }
		public override bool IsLeaf { get { return this._quadrants.Length == 0; } }
		public override IEnumerable<E> NodeElements { get {
			if (this.Depth < MaxDepth || this.NumMembers < CAPACITY)
				return this._members.Take(this.NumMembers);
			else
				return this._members.Concat(this._leftovers);
		} }

		protected abstract N Zero { get; }
		protected abstract N Add(N value1, N value2);
		protected abstract N Subtract(N value1, N value2);
		protected abstract N Halve(N value);

		private T[] _quadrants = Array.Empty<T>();
		private readonly E[] _members = new E[CAPACITY];//entries in non-leaves are leftovers that must be ignored
		private List<E> _leftovers;

		public AQuadTree(VectorDouble corner1, VectorDouble corner2, T parent = null) 
		: base(parent) {//make sure all values in x1 are smaller than x2 (the corners of a cubic volume)
			if (Enumerable.Range(0, corner1.Coordinates.Length).All(d => corner1.Coordinates[d] == corner2.Coordinates[d])) throw new ArgumentException("Domain has no volume");

			var orderedCornerPoints = corner1.Coordinates.Zip(corner2.Coordinates, (a, b) => new { Min = a < b ? a : b, Max = a < b ? b : a }).ToArray();
			this.LeftCorner = orderedCornerPoints.Select(t => t.Min).ToArray();
			this.RightCorner = orderedCornerPoints.Select(t => t.Max).ToArray();
			this.Center = corner1.Coordinates.Zip(corner2.Coordinates, (a, b) => (a + b) / 2d).ToArray();//average of each dimension
		}

		public override bool DoesContain(E element) {
			for (int d = 0; d < this.Dimensionality; d++)//all dimensions must overlap
				if (this.LeftCorner[d].CompareTo(element.Coordinates[d]) >0 || element.Coordinates[d].CompareTo(this.RightCorner[d]) > 0)
					return false;
			return true;//all dimensions overlap (or there are none)
		}

		protected override void AddElementToNode(E element) {
			if (this.NumMembers < CAPACITY) this._members[this.NumMembers] = element;
			else {
				if (this.Depth < MaxDepth) {
					if (this.NumMembers == CAPACITY) {//one-time quadrant formation
						this._quadrants = this.OrganizeNodes(this.FormNodes().ToArray());
						foreach (E member in this._members)
							this.GetContainingChild(member).Add(member);
					}
					this.GetContainingChild(element).Add(element);
				} else {
					this._leftovers ??= new List<E>();//one-time
					this._leftovers.Add(element);
				}
			}
			this.NumMembers++;
		}

		public override T GetContainingChild(E element) {
			int quadrantIdx = Enumerable.Range(0, this.Dimensionality).Sum(d => element.Coordinates[d].CompareTo(this.Center[d]) >= 0 ? 1 << d : 0);
			return this._quadrants[quadrantIdx];
		}

		protected abstract T NewNode(double[] cornerA, double[] cornerB, T parent);
		protected virtual T[] OrganizeNodes(T[] nodes) { return nodes; }

		private IEnumerable<T> FormNodes() {
			foreach (var newNodeData in Enumerable
				.Range(0, 1 << this.Dimensionality)//the 2^dimension "quadrants" of the Euclidean hyperplane
				.Select(q => {
					N[]
						sizeHalved = this.LeftCorner.Zip(this.RightCorner, (a, b) => this.Halve(this.Subtract(b, a))).ToArray(),
						cornerA = Enumerable
							.Range(0, this.Dimensionality)
							.Select(d => this.Add(
								this.LeftCorner[d],
								(q & (1 << d)) == 0
									? this.Zero
									: sizeHalved[d]))
							.ToArray();
					return new {
						LeftCorner = cornerA,
						RightCorner = cornerA.Zip(sizeHalved, (a, b) => this.Add(a, b)).ToArray(),
						Parent = (T)this };
				}))
				yield return this.NewNode(newNodeData.LeftCorner, newNodeData.RightCorner, newNodeData.Parent);
		}

		public override string ToString() {
			return string.Format("Node[<{0}> thru <{1}>][{2} members]",
				string.Join(", ", this.LeftCorner),
				string.Join(", ", this.RightCorner),
				this.NumMembers);
		}
	}

	public class QuadTree<E> : AQuadTree<E, QuadTree<E>>
	where E : IVector<double> {
		public QuadTree(VectorDouble corner1, VectorDouble corner2, QuadTree<E> parent = null)
		: base(corner1, corner2, parent) { }

		protected override double Zero => 0d;
		protected override double Add(double value1, double value2) { return value1 + value2; }
		protected override double Halve(double value) { return value / 2d; }
		protected override double Subtract(double value1, double value2) { return value1 - value2; }
	}

	public abstract class AQuadTreeFloat<V, T> : AQuadTree<float, V, T>
	where V : IVector<float>
	where T : AQuadTreeFloat<V, T>{
		public override int MaxDepth => 20;

		public AQuadTreeFloat(float[] corner1, float[] corner2, T parent = null)
		: base(corner1, corner2, parent) { }

		protected override float Zero => 0f;
		protected override float Add(float value1, float value2) { return value1 + value2; }
		protected override float Halve(float value) { return value / 2f; }
		protected override float Subtract(float value1, float value2) { return value1 - value2; }
	}
}