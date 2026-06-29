using System.Collections.Generic;

namespace Generic.Trees;

public class TreeNode {
	public int val;
	public TreeNode left;
	public TreeNode right;
	public TreeNode(int val=0, TreeNode left=null, TreeNode right=null) {
		this.val = val;
		this.left = left;
		this.right = right;
	}
}

public static class BinaryTreeTraversals {
	public static IEnumerable<int> InOrder(TreeNode root) {//left, self, right, [parent]
		TreeNode node = root;
		Stack<TreeNode> path = new();
        
		path.Push(node);
		while (!(node.left is null)) {
			node = node.left;
			path.Push(node);
		}
        
		while (path.TryPop(out node)) {
			yield return node.val;
            
			if (!(node.right is null)) {
				node = node.right;
				path.Push(node);
                
				while (!(node.left is null)) {
					node = node.left;
					path.Push(node);
				}
			}
		}
	}

	public static IEnumerable<int> ReverseOrder(TreeNode root) {//right, self, left, [parent]
		TreeNode node = root;
		Stack<TreeNode> path = new();
        
		path.Push(node);
		while (!(node.right is null)) {
			node = node.right;
			path.Push(node);
		}
        
		while (path.TryPop(out node)) {
			yield return node.val;
            
			if (!(node.left is null)) {
				node = node.left;
				path.Push(node);
                
				while (!(node.right is null)) {
					node = node.right;
					path.Push(node);
				}
			}
		}
	}

	public static bool InorderEquals_Lazy(TreeNode r1, TreeNode r2) {
		IEnumerator<int> enum1 = InOrder(r1).GetEnumerator(),
			enum2 = InOrder(r2).GetEnumerator();
		bool m1 = enum1.MoveNext(),
			m2 = enum2.MoveNext();

		while (m1 || m2) {
			if (m1 ^ m2 || enum1.Current != enum2.Current)
				return false;

			m1 = enum1.MoveNext();
			m2 = enum2.MoveNext();
		}

		return true;
	}

	public static bool InorderEquals(TreeNode r1, TreeNode r2) {//puke version
		TreeNode node1 = r1, node2 = r2;
		Stack<TreeNode> path1 = new(), path2 = new();
        
		path1.Push(node1);
		path2.Push(node2);
		while (!(node1.left is null)) {
			node1 = node1.left;
			path1.Push(node1);
		}
		while (!(node2.left is null)) {
			node2 = node2.left;
			path2.Push(node2);
		}

		bool pop1 = path1.TryPop(out node1),
			pop2 = path2.TryPop(out node2);
        
		while (pop1 || pop2) {
			if (pop1 ^ pop2 || node1.val != node2.val)
				return false;
            
			if (!(node1.right is null)) {
				node1 = node1.right;
				path1.Push(node1);
                
				while (!(node1.left is null)) {
					node1 = node1.left;
					path1.Push(node1);
				}
			}
            
			if (!(node2.right is null)) {
				node2 = node2.right;
				path2.Push(node2);
                
				while (!(node2.left is null)) {
					node2 = node2.left;
					path2.Push(node2);
				}
			}

			pop1 = path1.TryPop(out node1);
			pop2 = path2.TryPop(out node2);
		}

		return true;
	}
}