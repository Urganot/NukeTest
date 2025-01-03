namespace NukeTest.TestModule2;

using TestModule1;

public class TestClass2
{
    public static void DoSomething()
    {
        Console.WriteLine("I was called");
    }

    public static void IllegalMethod()
    {
        TestClass1.DoSomething();
    }
}