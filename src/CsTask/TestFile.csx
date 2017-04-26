using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text.RegularExpressions;


//Base Members

public void TestPublicMethodInstance()
{
	var t = "FDS";
}

void TestMethodInstance()
{
	var t = "FDS";
}

static void TestMethodStatic()
{
	int x = 500;
}


int TestFieldInstance = 5;

static int TestFieldStatic = 15;

int TestPropertyInstance { get; set; } = 100;

static int TestPropertyStatic { get; set; } = 3453;


//Output as Json Test

class TestOutputClass
{
	public string X { get; set; }
	public string Y { get; set; }
	public string Z { get; set; }
}

TestOutputClass TestOutput()
{
	TestOutputClass toc = new TestOutputClass
	{
		X = "HELLO",
		Y = "WORLD",
		Z = null
	};
	return toc;
}

//Pipe Test

int TestPipeInput()
{
	return 22;
}

int TestPipeOutput(int input)
{
	return input;
}

//C#7 features test

string CS7Test()
{
	int TestLocalFunc()
	{
		return 25;
	}

	int F = TestLocalFunc();
	string literalFormattedString = $@"AHAHAHAHAHAHA {F}
LOL
{F}
";
	return literalFormattedString;
}


//Linq Test

string TestLinq()
{
	List<string> codes = new List<string>
	{
		"A",
		"B",
		"C"
	};

	return codes.FirstOrDefault();
}

//Regex Test

string TestRegex()
{
	string hostFileTest = "255.125.254.444    TEST";
	Regex reg = new Regex(@"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\s+(\S+)");

	var match = reg.Match(hostFileTest);
	return match.Value;
}