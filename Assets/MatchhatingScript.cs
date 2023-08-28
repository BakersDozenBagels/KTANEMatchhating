using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

public class MatchhatingScript : MonoBehaviour
{
    [SerializeField]
    private Decoration[] _lineDecorations;
    [SerializeField]
    private GameObject _lineBase, _lineCap, _prefabHolder, _skaia;
    [SerializeField]
    private KMSelectable[] _starSignButtons, _suitButtons;
    [SerializeField]
    private Color _buttonColor, _selectedColor;
    [SerializeField]
    private TextMesh _errorText;

    private int _id = ++_idc;
    private static int _idc;
    private Puzzle _puzzle;
    private bool _isSolved;

    private readonly List<GameObject> _selected = new List<GameObject>();
    private readonly List<GameObject> _lines = new List<GameObject>();
    private readonly List<Connection> _connections = new List<Connection>();
    private readonly List<string> _animations = new List<string>();

    // Suits (4) then star signs (12)
    private const string Symbols = "\u2660\u2665\u2663\u2666\u2648\u2649\u264a\u264b\u264c\u264d\u264e\u264f\u2650\u2651\u2652\u2653";

    private void Awake()
    {
        _skaia.transform.localScale = Vector3.zero;
    }

    private void Start()
    {
        _prefabHolder.SetActive(false);
        foreach (var c in _starSignButtons)
        {
            c.OnInteract += () => { PressStarSign(c.gameObject); return false; };
            c.GetComponent<Renderer>().material.color = _buttonColor;
        }

        //    StartCoroutine(Temp());
        //}
        //private IEnumerator Temp()
        //{
        _puzzle = Puzzle.GenerateSimple();
        StartCoroutine(Watch(_puzzle.GenerateSolution()));
        //int it = 0;
        //do
        //{
        //    var t = Time.time;
        //    try
        //    {
        //        _puzzle = Puzzle.Generate(Log, 0);
        //    }
        //    catch(Exception) { }
        //    yield return null;
        //    DebugLog((++it).ToString() + " " + (Time.time - t).ToString());
        //    GetComponent<KMAudio>().PlaySoundAtTransform("wait", transform);
        //    yield return new WaitForSeconds(10f);
        //}
        ////while(false);
        //while(_puzzle == null);

        Log("Generated puzzle: " + Enumerable.Range(0, 8).Select(i => (_puzzle.Colors[i] == Puzzle.TrollColor.Black ? "K" : _puzzle.Colors[i].ToString().Substring(0, 1)) + Symbols[4 + _puzzle.StarSigns[i]]).Join(", "));
        Log("(Trying to find a solution...)");

        var assignments = _puzzle.StarSigns.Select(i => Symbols[i + 4]).ToArray();
        var colors = _puzzle.Colors.Select(Puzzle.ColorFrom).ToArray();
        for (int i = 0; i < 8; i++)
        {
            _starSignButtons[i].GetComponentInChildren<TextMesh>().text = assignments[i].ToString();
            _starSignButtons[i].GetComponentInChildren<TextMesh>().color = colors[i];
        }

        for (int i = 0; i < 4; i++)
        {
            int j = i;
            _suitButtons[j].OnInteract += () => { PressSuit(j); return false; };
        }
    }

    private IEnumerator Watch(Puzzle.Work<Connection[]> w)
    {
        while (true)
        {
            for (int i = 0; i < 10; i++)
                if (!w.Coroutine.MoveNext())
                    goto finished;
            if(_isSolved)
            {
                DebugLog("Module solved, stopping solution search.");
                yield break;
            }
            yield return w.Coroutine.Current;
        }
    finished:
        if (_isSolved)
            DebugLog("Found solution after module solved.");
        if (w.HasResult)
            Log("A valid solution is: " + w.Result.Select(c => c.ToString(true)).Join(", "));
        else
        {
            Log("No valid solutions appear to exist. Please report this bug.");
            GetComponent<KMBombModule>().HandlePass();
            _isSolved = true;
            _errorText.text = "!";
            throw new UnreachableException();
        }
    }

    private void Log(object o)
    {
        Debug.Log("[Matchhating #" + _id + "] " + o.ToString());
    }

    private T DebugLog<T>(T o)
    {
        Debug.Log("<Matchhating #" + _id + "> " + o.ToString());
        return o;
    }

    private void PressStarSign(GameObject button)
    {
        button.GetComponent<KMSelectable>().AddInteractionPunch(0.1f);
        GetComponent<KMAudio>().PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);

        if (_isSolved)
            return;

        if (_selected.Contains(button))
        {
            _selected.Remove(button);
            button.GetComponent<Renderer>().material.color = _buttonColor;
            return;
        }

        _selected.Add(button);
        button.GetComponent<Renderer>().material.color = _selectedColor;

        //        if (_selected.Count == 2)
        //        {
        //            var swap = _selected[0].name.CompareTo(_selected[1].name) > 0;
        //            var id = _selected[swap ? 1 : 0].name + _selected[swap ? 0 : 1].name;
        //            if (_lines.Any(g => g.name == id))
        //            {
        //#warning Sound doesn't exist
        //                GetComponent<KMAudio>().PlaySoundAtTransform("eraseLine", transform);
        //                ToggleLine(_selected[0], _selected[1]);

        //                _selected[0].GetComponent<Renderer>().material.color = _buttonColor;
        //                _selected[1].GetComponent<Renderer>().material.color = _buttonColor;
        //                _selected.Clear();
        //            }
        //        }
    }

    private void PressSuit(int suit)
    {
        _suitButtons[suit].AddInteractionPunch(0.1f);

        if (_isSolved)
            return;

        if (_selected.Count != 2)
        {
            GetComponent<KMAudio>().PlaySoundAtTransform("badPress", _suitButtons[suit].transform);
            return;
        }

        var id = new Connection(byte.Parse(_selected[0].name.Replace("Button", "")), byte.Parse(_selected[1].name.Replace("Button", "")));
        if (_animations.Contains(id))
        {
            GetComponent<KMAudio>().PlaySoundAtTransform("wait", _suitButtons[suit].transform);
            return;
        }

        GetComponent<KMAudio>().PlaySoundAtTransform("drawLine" + Random.Range(1, 5), _suitButtons[suit].transform);
        ToggleLine(_selected[0], _selected[1], suit);

        _selected[0].GetComponent<Renderer>().material.color = _buttonColor;
        _selected[1].GetComponent<Renderer>().material.color = _buttonColor;
        _selected.Clear();
    }

    private void ToggleLine(GameObject ga, GameObject gb, int style = 0)
    {
        var id = new Connection(byte.Parse(_selected[0].name.Replace("Button", "")), byte.Parse(_selected[1].name.Replace("Button", "")));
        var a = ga.transform.localPosition;
        var b = gb.transform.localPosition;
        var match = _lines.FirstOrDefault(g => g.name == id);
        if (match == null)
            StartCoroutine(AnimateNewLine(id, a, b, style));
        else
            StartCoroutine(AnimateRemoveLine(match));
    }

    private IEnumerator AnimateRemoveLine(GameObject line, float delay = 0f)
    {
        if (delay != 0f)
            yield return new WaitForSeconds(delay);

        _animations.Add(line.name);

        const float duration = 0.25f;
        Func<float, float> ease = i => Easing.InQuad(i, 0f, 1f, 1f);

        float t = Time.time;
        while (Time.time - t < duration)
        {
            yield return null;
            line.transform.localScale = new Vector3(Mathf.Lerp(1f, 0f, ease((Time.time - t) / duration)), 1f, 1f);
        }
        _lines.Remove(line);
        _animations.Remove(line.name);
        _connections.Remove((Connection)line.name);
        Validate();
        Destroy(line);
    }

    private IEnumerator AnimateNewLine(Connection id, Vector3 a, Vector3 b, int style)
    {
        _animations.Add(id);

        const float backingHeight = 0.01516541f;
        const float buttonDiameter = 0.025f;
        const float capLength = 0.006f;
        const float decorationLength = 0.005f;
        const float lineLength = 0.01f;
        const float gapSize = buttonDiameter + 2f * capLength + 2f * decorationLength;
        const float segmentLength = lineLength / 2f;

        const float introDuration = 0.2f;
        Func<float, float> introEase = i => Easing.OutQuad(i, 0f, 1f, 1f);

        const float growDuration = 0.6f;
        Func<float, float> growEase = i => Easing.InOutQuad(i, 0f, 1f, 1f);

        const float decorateDuration = 1f;
        Func<float, float> decorateEase = i => Easing.InOutQuad(i, 0f, 1f, 1f);

        float fudge = (int.Parse(id) - 11f) / 77000f;
        var parent = new GameObject(id);
        parent.transform.parent = transform;
        parent.transform.localPosition = new Vector3((a.x + b.x) / 2f, backingHeight + fudge, (a.z + b.z) / 2f);
        parent.transform.localScale = new Vector3(0f, 1f, 1f);
        parent.transform.localRotation = Quaternion.FromToRotation(new Vector3(0f, 0f, 1f), new Vector3(a.x - b.x, 0f, a.z - b.z));

        var endDistance = (new Vector2(a.x - b.x, a.z - b.z).magnitude - gapSize) / segmentLength;

        _lines.Add(parent);
        var c = (Connection)parent.name;
        c.SuitUsed = (Connection.Suit)style;
        _connections.Add(c);
        Validate();

        var line = Instantiate(_lineBase);
        line.transform.parent = parent.transform;
        line.transform.localScale = new Vector3(1f, 1f, 0f);
        line.transform.localRotation = Quaternion.identity;
        line.transform.localPosition = Vector3.zero;
        line.GetComponentInChildren<Renderer>().material.color = _lineDecorations[style].Color;
        var capA = Instantiate(_lineCap);
        capA.transform.parent = parent.transform;
        capA.transform.localScale = Vector3.one;
        capA.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        capA.transform.localPosition = Vector3.zero;
        capA.GetComponentInChildren<Renderer>().material.color = _lineDecorations[style].Color;
        var capB = Instantiate(_lineCap);
        capB.transform.parent = parent.transform;
        capB.transform.localScale = Vector3.one;
        capB.transform.localRotation = Quaternion.identity;
        capB.transform.localPosition = Vector3.zero;
        capB.GetComponentInChildren<Renderer>().material.color = _lineDecorations[style].Color;

        float t = Time.time;
        while (Time.time - t < introDuration)
        {
            yield return null;
            parent.transform.localScale = Vector3.Lerp(new Vector3(0f, 1f, 1f), Vector3.one, introEase((Time.time - t) / introDuration));
        }
        parent.transform.localScale = Vector3.one;

        t = Time.time;
        while (Time.time - t < growDuration)
        {
            yield return null;
            var scale = Mathf.Lerp(0f, endDistance, growEase((Time.time - t) / growDuration));
            var scale2 = scale * segmentLength;
            line.transform.localScale = new Vector3(1f, 1f, scale);
            capA.transform.localPosition = new Vector3(0f, 0f, scale2 * 0.5f);
            capB.transform.localPosition = new Vector3(0f, 0f, scale2 * -0.5f);
        }
        line.transform.localScale = new Vector3(1f, 1f, endDistance);
        capA.transform.localPosition = new Vector3(0f, 0f, endDistance * segmentLength * 0.5f);
        capB.transform.localPosition = new Vector3(0f, 0f, endDistance * segmentLength * -0.5f);

        var decorA = Instantiate(_lineDecorations[style].Cap);
        decorA.transform.parent = parent.transform;
        decorA.transform.localScale = new Vector3(0f, 1f, 1f);
        decorA.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        decorA.GetComponentInChildren<Renderer>().material.color = _lineDecorations[style].Color;
        decorA.transform.localPosition = new Vector3(0f, 0f, endDistance * segmentLength * 0.5f + capLength);
        var decorB = Instantiate(_lineDecorations[style].Cap);
        decorB.transform.parent = parent.transform;
        decorB.transform.localScale = new Vector3(0f, 1f, 1f);
        decorB.transform.localRotation = Quaternion.identity;
        decorB.GetComponentInChildren<Renderer>().material.color = _lineDecorations[style].Color;
        decorB.transform.localPosition = new Vector3(0f, 0f, -(endDistance * segmentLength * 0.5f + capLength));

        t = Time.time;
        while (Time.time - t < decorateDuration)
        {
            yield return null;
            var scale = Mathf.Lerp(0f, 1f, decorateEase((Time.time - t) / decorateDuration));
            decorA.transform.localScale = new Vector3(scale, 1f, 1f);
            decorB.transform.localScale = new Vector3(scale, 1f, 1f);
        }

        _animations.Remove(id);
    }

    private void Validate()
    {
        if (_isSolved)
            return;

        //DebugLog(_connections.Join(" "));

        //if (DebugLog(_puzzle.Validate(_connections, i => DebugLog(i))) == 1)
        if (_puzzle.Validate(_connections) == 1)
        {
            Log("Good job. Module solved.");
            GetComponent<KMBombModule>().HandlePass();
            _isSolved = true;
            StartCoroutine(SolveAnimation());
        }
    }

    private IEnumerator SolveAnimation()
    {
        while (_animations.Count > 0)
            yield return null;
        GetComponent<KMAudio>().PlaySoundAtTransform("Solve", transform);
        //var delays = Enumerable.Range(0, _connections.Count).Select(_ => Random.value * 3f).ToArray();
        //for (int i = 0; i < _connections.Count; i++)
        //{
        //    var match = _lines.FirstOrDefault(g => g.name == _connections[i]);
        //    StartCoroutine(AnimateRemoveLine(match, delays[i]));
        //}
        //while (_animations.Count > 0)
        //    yield return null;
        StartCoroutine(SpinSkaia());
        float t = Time.time;
        const float delay = 4f;
        while(Time.time - t < delay)
        {
            _skaia.transform.localScale = Vector3.one * Easing.InOutQuad(Time.time - t, 0f, 0.17f, delay);
            yield return null;
        }
        _skaia.transform.localScale = Vector3.one * 0.17f;
    }

    private IEnumerator SpinSkaia()
    {
        float rot = 0f;
        while (true)
        {
            rot += Time.deltaTime * 30f;
            _skaia.transform.localEulerAngles = new Vector3(90f, rot, 0f);
            yield return null;
        }
    }

    [Serializable]
    public class Decoration
    {
        public GameObject Cap;
        public Color Color;
    }

    internal struct Connection : IEquatable<Connection>
    {
        public enum Suit : byte
        {
            Spades = 0,
            Hearts = 1,
            Clubs = 2,
            Diamonds = 3
        }
        public byte Lower, Upper;
        public Suit SuitUsed;

        public Connection(int a, int b, Suit s = Suit.Spades)
        {
            Lower = (byte)Mathf.Min(a, b);
            Upper = (byte)Mathf.Max(a, b);
            SuitUsed = s;
        }

        public override string ToString()
        {
            return Lower.ToString() + Upper.ToString();
        }

        public string ToString(bool useSuit)
        {
            if (!useSuit)
                return ToString();
            return ToString() + " " + SuitUsed.ToString().Substring(0, 1);
        }

        public override bool Equals(object obj)
        {
            return obj is Connection && Equals((Connection)obj);
        }

        public bool Equals(Connection other)
        {
            return Lower == other.Lower &&
                   Upper == other.Upper;
        }

        public override int GetHashCode()
        {
            var hashCode = 1595623102;
            hashCode = hashCode * -1521134295 + Lower.GetHashCode();
            hashCode = hashCode * -1521134295 + Upper.GetHashCode();
            return hashCode;
        }

        public static implicit operator string(Connection c)
        {
            return c.ToString();
        }

        public static explicit operator Connection(string c)
        {
            if (c.Length != 2)
                throw new FormatException("Expected a well-formed string, got \"" + c + "\"");
            return new Connection(byte.Parse(c[0].ToString()), byte.Parse(c[1].ToString()));
        }

        public static bool operator ==(Connection connection1, Connection connection2)
        {
            return connection1.Equals(connection2);
        }

        public static bool operator !=(Connection connection1, Connection connection2)
        {
            return !(connection1 == connection2);
        }

        public bool Intersects(Connection other)
        {
            return _intersections[this].Contains(other);
        }

        private static readonly Dictionary<Connection, Connection[]> _intersections = new Dictionary<string, string[]>()
        {
            { "12", new string[]{ } },
            { "13", new string[]{ "24", "25", "26", "27", "28" } },
            { "14", new string[]{ "25", "26", "27", "28", "35", "36", "37", "38" } },
            { "15", new string[]{ "26", "27", "28", "36", "37", "38", "46", "47", "48" } },
            { "16", new string[]{ "27", "28", "37", "38", "47", "48", "57", "58" } },
            { "17", new string[]{ "28", "38", "48", "58", "68" } },
            { "18", new string[]{ } },
            { "23", new string[]{ } },
            { "24", new string[]{ "35", "36", "37", "38", "31" } },
            { "25", new string[]{ "36", "37", "38", "31", "46", "47", "48", "41" } },
            { "26", new string[]{ "37", "38", "31", "47", "48", "41", "57", "58", "51" } },
            { "27", new string[]{ "38", "31", "48", "41", "58", "51", "68", "61" } },
            { "28", new string[]{ "31", "41", "51", "61", "71" } },
            { "34", new string[]{ } },
            { "35", new string[]{ "46", "47", "48", "41", "42" } },
            { "36", new string[]{ "47", "48", "41", "42", "57", "58", "51", "52" } },
            { "37", new string[]{ "48", "41", "42", "58", "51", "52", "68", "61", "62" } },
            { "38", new string[]{ "41", "42", "51", "52", "61", "62", "71", "72" } },
            { "45", new string[]{ } },
            { "46", new string[]{ "57", "58", "51", "52", "53" } },
            { "47", new string[]{ "58", "51", "52", "53", "68", "61", "62", "63" } },
            { "48", new string[]{ "51", "52", "53", "61", "62", "63", "71", "72", "73" } },
            { "56", new string[]{ } },
            { "57", new string[]{ "68", "61", "62", "63", "64" } },
            { "58", new string[]{ "61", "62", "63", "64", "71", "72", "73", "74" } },
            { "67", new string[]{ } },
            { "68", new string[]{ "71", "72", "73", "74", "75" } },
            { "78", new string[]{ } }
        }.ToDictionary(kvp => (Connection)kvp.Key, kvp => kvp.Value.Select(s => (Connection)s).ToArray());
    }

    private class Puzzle
    {
        public enum TrollColor
        {
            Black = 0,
            Red,
            Green,
            Blue
        }

        public readonly int[] StarSigns;
        public readonly TrollColor[] Colors;
        private Func<IEnumerable<Connection>, int>[] _conditions;

        private Puzzle(int[] signs, TrollColor[] colors)
        {
            StarSigns = signs;
            Colors = colors;
        }

        public static Puzzle Boring()
        {
            var p = new Puzzle(Enumerable.Range(0, 8).ToArray(), Enumerable.Repeat(TrollColor.Black, 8).ToArray());
            p._conditions = new Func<IEnumerable<Connection>, int>[] { _ => 1 };
            return p;
        }

        public static Puzzle GenerateSimple()
        {
            var suits = Enum.GetValues(typeof(Connection.Suit)).Cast<Connection.Suit>().ToArray();
            var a = Random.Range(0, 1) == 0 ? new int[] { 3 } : new int[] { 0, 6 };
            var b = Random.Range(0, 1) == 0 ? new int[] { 5 } : new int[] { 2, 8 };
            var values = new int[] { 1, 4, 7 }.Concat(a).Concat(b).OrderBy(_ => Random.value).Take(4).ToArray();
            var selected = Enumerable.Range(0, 4).Select(i => new { sign = values[i] / 3 + (int)suits[i] * 3, color = values[i] % 3 + 1 }).ToArray();
            selected = selected.Concat(Enumerable.Range(0, 12).Except(selected.Select(s => s.sign)).OrderBy(_ => Random.value).Take(4).Select(s => new { sign = s, color = 0 })).OrderBy(_ => Random.value).ToArray();

            var p = new Puzzle(selected.Select(s => s.sign).ToArray(), selected.Select(s => (TrollColor)s.color).ToArray());
            p._conditions = Enumerable.Range(0, 8).Select(p.GetCondition).ToArray();
            return p;
        }

        public static Puzzle Generate(Action<object> logger = null, int coloredCount = 0)
        {
            if (logger == null)
                logger = _ => { };
            int[] signs;
            TrollColor[] colors;
            Puzzle p;
            int iter = 0;
            do
            {
                iter++;
                signs = Enumerable.Range(0, 12).OrderBy(_ => Random.value).Take(8).ToArray();
                colors = Enumerable.Range(0, 8).Select(i => i >= coloredCount ? TrollColor.Black : (TrollColor)Random.Range(1, 4)).OrderBy(_ => Random.value).ToArray();
                p = new Puzzle(signs, colors);
                p._conditions = Enumerable.Range(0, 8).Select(p.GetCondition).ToArray();
                Stack<Node> stack = new Stack<Node>();
                stack.Push(new Node(p, new List<Connection>(), 0));
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    var check = cur.Check();
                    if (check == 1)
                        goto done;
                    if (check == -1)
                        continue;
                    foreach (var n in cur.Move())
                        stack.Push(n);
                }
                //if (iter > 10)
                break;
            }
            while (true);
            logger("Failed to generate.");
            throw new Exception();

        done:
            logger("Generation complete in " + iter + " attempts.");
            return p;
        }

        public Work<Connection[]> GenerateSolution()
        {
            return new SolutionFinder(this);
        }

        public int Validate(IEnumerable<Connection> li, Action<object> logger = null)
        {
            if (logger == null)
                logger = _ => { };
            var lines = li.ToList();
            if (lines.Count == 0)
                return 0;
            var en = _conditions.Select(f => f(lines));
            bool flag = true;
            foreach (var cond in en)
            {
                if (cond == -1)
                    return -1;
                if (cond == 0)
                    flag = false;
            }
            if (!flag)
                return 0;
            if (Enumerable
                .Range(1, 8)
                .All(i =>
                {
                    logger("check " + i);
                    var lin = lines
                        .Where(l => l.Lower == i || l.Upper == i)
                        .Select(l => l.SuitUsed)
                        .OrderBy(s => (byte)s)
                        .ToArray();
                    logger(lin.Join(" "));
                    return lin
                        .SequenceEqual(new byte[] { 0, 1, 2, 3 }.Cast<Connection.Suit>());
                }))
                return 1;
            return 0;
        }

        private Func<IEnumerable<Connection>, int> GetCondition(int index)
        {
            if (Colors[index] == TrollColor.Black)
                return _ => 1;
            switch (Colors[index])
            {
                case TrollColor.Red:
                    switch (Mode(StarSigns[index]))
                    {
                        case 0: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : cs.None(c2 => c2.SuitUsed == Connection.Suit.Spades && c.Intersects(c2)) ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                        case 1: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : cs.Any(c2 => (c2.SuitUsed == Connection.Suit.Spades || c2.SuitUsed == Connection.Suit.Clubs) && c.Intersects(c2)) ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                        case 2: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : cs.None(c2 => c2.SuitUsed == Connection.Suit.Clubs && c.Intersects(c2)) ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                    }
                    break;
                case TrollColor.Green:
                    switch (Mode(StarSigns[index]))
                    {
                        case 0: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : c.Upper - c.Lower == 1 || c.Upper - c.Lower == 7 ? -1 : 1).Any(a => a == -1) ? -1 : 1;
                        case 1: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : c.Upper - c.Lower == 4 ? -1 : 1).Any(a => a == -1) ? -1 : 1;
                        case 2: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : c.Upper - c.Lower == 1 || c.Upper - c.Lower == 7 || c.Upper - c.Lower == 4 ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                    }
                    break;
                case TrollColor.Blue:
                    switch (Mode(StarSigns[index]))
                    {
                        case 0: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : cs.None(c2 => c2.SuitUsed == Connection.Suit.Hearts && c.Intersects(c2)) ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                        case 1: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : cs.Any(c2 => (c2.SuitUsed == Connection.Suit.Hearts || c2.SuitUsed == Connection.Suit.Diamonds) && c.Intersects(c2)) ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                        case 2: return cs => cs.Select(c => c.SuitUsed != Suit(StarSigns[index]) ? 0 : cs.None(c2 => c2.SuitUsed == Connection.Suit.Diamonds && c.Intersects(c2)) ? 1 : -1).Any(a => a == -1) ? -1 : 1;
                    }
                    break;
            }

            throw new UnreachableException();
        }

        private static int Mode(int sign)
        {
            return sign % 3;
        }

        private static Connection.Suit Suit(int sign)
        {
            return (Connection.Suit)(sign / 3);
        }

        public static Color ColorFrom(TrollColor c)
        {
            switch (c)
            {
                case TrollColor.Red:
                    return Color.red;
                case TrollColor.Green:
                    return Color.green;
                case TrollColor.Blue:
                    return Color.blue;
                default:
                    return Color.black;
            }
        }

        private class Node
        {
            public readonly Puzzle P;
            public readonly ReadOnlyCollection<Connection> C;
            public readonly int Depth;

            public Node(Puzzle p, List<Connection> c, int d)
            {
                P = p;
                C = c.AsReadOnly();
                Depth = d;
            }

            private static string Connect(int index)
            {
                switch (index)
                {
                    case 0:
                        return "12";
                    case 1:
                        return "13";
                    case 2:
                        return "14";
                    case 3:
                        return "15";
                    case 4:
                        return "16";
                    case 5:
                        return "17";
                    case 6:
                        return "18";
                    case 7:
                        return "23";
                    case 8:
                        return "24";
                    case 9:
                        return "25";
                    case 10:
                        return "26";
                    case 11:
                        return "27";
                    case 12:
                        return "28";
                    case 13:
                        return "34";
                    case 14:
                        return "35";
                    case 15:
                        return "36";
                    case 16:
                        return "37";
                    case 17:
                        return "38";
                    case 18:
                        return "45";
                    case 19:
                        return "46";
                    case 20:
                        return "47";
                    case 21:
                        return "48";
                    case 22:
                        return "56";
                    case 23:
                        return "57";
                    case 24:
                        return "58";
                    case 25:
                        return "67";
                    case 26:
                        return "68";
                    case 27:
                        return "78";
                }
                throw new UnreachableException();
            }

            public IEnumerable<Node> Move()
            {
                if (Depth >= 28)
                    yield break;
                foreach (var suit in new Connection.Suit[] { Connection.Suit.Spades, Connection.Suit.Hearts, Connection.Suit.Clubs, Connection.Suit.Diamonds })
                {
                    var con = (Connection)Connect(Depth);
                    con.SuitUsed = suit;
                    if (C.Contains(con))
                        continue;
                    var relatedA = C.Where(c => c.Lower == con.Lower || c.Upper == con.Lower).ToList();
                    if (relatedA.Any(c => c.SuitUsed == suit))
                        continue;
                    var relatedB = C.Where(c => c.Lower == con.Upper || c.Upper == con.Upper).ToList();
                    if (relatedB.Any(c => c.SuitUsed == suit))
                        continue;
                    var co = C.ToList();
                    co.Add(con);
                    yield return new Node(P, co, Depth + 1);
                }
                yield return new Node(P, C.ToList(), Depth + 1);

                //yield break;
                //for(int i = Depth; i < 8; i++)
                //{
                //    if(i == Depth)
                //        continue;
                //    if(C.Contains(new Connection(i + 1, Depth + 1)))
                //        continue;
                //    var related = C.Where(c => c.Lower - 1 == i || c.Lower - 1 == Depth || c.Upper - 1 == i || c.Upper - 1 == Depth).ToList();
                //    if(!related.Any(c => c.SuitUsed == Connection.Suit.Spades) && C.Count(c => c.SuitUsed == Connection.Suit.Spades) < 4)
                //    {
                //        var c = C.ToList();
                //        c.Add(new Connection(i + 1, Depth + 1, Connection.Suit.Spades));
                //        yield return new Node(P, c, Depth + 1);
                //    }
                //    if(!related.Any(c => c.SuitUsed == Connection.Suit.Hearts) && C.Count(c => c.SuitUsed == Connection.Suit.Hearts) < 4)
                //    {
                //        var c = C.ToList();
                //        c.Add(new Connection(i + 1, Depth + 1, Connection.Suit.Hearts));
                //        yield return new Node(P, c, Depth + 1);
                //    }
                //    if(!related.Any(c => c.SuitUsed == Connection.Suit.Clubs) && C.Count(c => c.SuitUsed == Connection.Suit.Clubs) < 4)
                //    {
                //        var c = C.ToList();
                //        c.Add(new Connection(i + 1, Depth + 1, Connection.Suit.Clubs));
                //        yield return new Node(P, c, Depth + 1);
                //    }
                //    if(!related.Any(c => c.SuitUsed == Connection.Suit.Diamonds) && C.Count(c => c.SuitUsed == Connection.Suit.Diamonds) < 4)
                //    {
                //        var c = C.ToList();
                //        c.Add(new Connection(i + 1, Depth + 1, Connection.Suit.Diamonds));
                //        yield return new Node(P, c, Depth + 1);
                //    }
                //}
            }

            public int Check()
            {
                return P.Validate(C);
            }
        }

        public abstract class Work<T>
        {
            public IEnumerator Coroutine { get; protected set; }
            public T Result { get; protected set; }
            public bool HasResult { get; protected set; }
        }

        public sealed class SolutionFinder : Work<Connection[]>
        {
            private Puzzle _puzzle;
            public SolutionFinder(Puzzle p)
            {
                _puzzle = p;
                Coroutine = Run();
            }

            private IEnumerator Run()
            {
                Stack<Node> stack = new Stack<Node>();
                stack.Push(new Node(_puzzle, new List<Connection>(), 0));
                while (stack.Count > 0)
                {
                    var cur = stack.Pop();
                    var check = cur.Check();
                    if (check == 1)
                    {
                        Result = cur.C.ToArray();
                        HasResult = true;
                        yield break;
                    }
                    if (check == -1)
                        continue;
                    foreach (var n in cur.Move())
                        stack.Push(n);
                    yield return null;
                }
            }
        }
    }
}

[Serializable]
class UnreachableException : Exception
{
    public UnreachableException()
    {
    }

    public UnreachableException(string message) : base(message)
    {
    }

    public UnreachableException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected UnreachableException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

static class Ex
{
    public static bool None<T>(this IEnumerable<T> en, Func<T, bool> selector)
    {
        foreach (bool b in en.Select(selector))
            if (b)
                return false;
        return true;
    }
}