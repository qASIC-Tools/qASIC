namespace Tests;

public static class qAssert
{
    public static void IsTrue(bool condition)
    {
        if (!condition)
            throw new qAssertException("Condition was not met!");
    }

    public static void IsTrue(bool condition, string message)
    {
        if (!condition)
            throw new qAssertException(message);
    }

    public static void IsFalse(bool condition)
    {
        if (condition)
            throw new qAssertException("Invalid condition was met!");
    }

    public static void IsFalse(bool condition, string message)
    {
        if (condition)
            throw new qAssertException(message);
    }

    public static void CompareCollections<T>(IEnumerable<T> collection1, IEnumerable<T> collection2)
    {
        var list1 = collection1.ToList();
        var list2 = collection2.ToList();
        if (list1.Count != list2.Count)
            throw new qAssertException($"Collections were of a different length ({list1.Count}, {list2.Count})!");
        
        for (int i = 0; i < list1.Count; i++)
            if (Comparer<T>.Default.Compare(list1[i], list2[i]) != 0)
                throw new qAssertException($"Items at index {i} were missmatched ({list1[i]}, {list2[i]}).");
    }
}
