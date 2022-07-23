using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class identifracScript : MonoBehaviour
{

    //public stuff
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public List<MeshRenderer> ButtonMesh;
    public List<GameObject> Tiles;
    public KMBombModule Module;

    //private stuff
    private readonly List<int>[] order = new List<int>[] { new List<int> { 1, 0, 3, 2, 5, 4, 7, 6 } /*x-flip*/, new List<int> { 2, 3, 0, 1, 6, 7, 4, 5 } /*y-flip*/, new List<int> { 4, 5, 6, 7, 0, 1, 2, 3 } /*z-flip*/, new List<int> { 0, 2, 1, 3, 4, 6, 5, 7 } /*bl-fl*/, new List<int> { 0, 4, 1, 5, 2, 6, 3, 7 } /*blf-ccw*/ };
    private List<int> seed = new List<int> { 48, 48, 48, 0, 0, 0, 0, 0 };
    List<List<int>> type = new List<List<int>> { new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 } };
    List<List<int>> coords = new List<List<int>> { new List<int> { 0, 0, 0 } };
    int skipme = 0;
    int depth = 1;
    private List<int> input = new List<int> { };
    private bool in2 = false;
    private List<bool> buttoncol = new List<bool> { }; 
    private bool solved = false;

    private List<List<GameObject>> layers;

    //logging
    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    private KMSelectable.OnInteractHandler Press(int pos)
    {
        return delegate
        {
            if (!solved)
            {
                Buttons[pos].AddInteractionPunch(.5f);
                if (in2)
                {
                    input[input.Count() - 1] = input.Last() * 7 + pos;
                    in2 = false;
                    Audio.PlaySoundAtTransform("Beep2", Buttons[pos].transform);
                    if (input.Count() == 8)
                    {
                        bool good = true;
                        for (int i = 0; i < 8; i++)
                            good &= (seed[i] == input[i]);
                        if (good)
                        {
                            Debug.LogFormat("[Identifrac #{0}] You submitted {1}. Solve!", _moduleID, input.Select(x => "" + (x / 7) + (x % 7)).Join(", "));
                            solved = true;
                            for (int i = 0; i < 7; i++)
                                ButtonMesh[i].material.color = new Color(0, 1, 0);
                            Module.HandlePass();
                            StartCoroutine(BackIterateVisual());
                        }
                        else
                        {
                            Debug.LogFormat("[Identifrac #{0}] You submitted {1}, where I expected {2}. Strike!", _moduleID, input.Select(x => "" + (x / 7) + (x % 7)).Join(", "), seed.Select(x => "" + (x / 7) + (x % 7)).Join(", "));
                            Module.HandleStrike();
                            input = new List<int> { };
                        }
                    }
                }
                else
                {
                    input.Add(pos);
                    in2 = true;
                    Audio.PlaySoundAtTransform("Beep1", Buttons[pos].transform);
                }
            }
            return false;
        };
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < Buttons.Length; i++)
        {
            buttoncol.Add(false);
            Buttons[i].OnInteract += Press(i);
            int x = i;
            Buttons[i].OnHighlight += delegate { if (!solved) { buttoncol[x] = true; } };
            Buttons[i].OnHighlightEnded += delegate { if (!solved) { buttoncol[x] = false; } };
            ButtonMesh.Add(Buttons[i].GetComponent<MeshRenderer>());
        }
    }

    void Start()
    {
        while (IsSymmetric(2))
            seed = seed.Select(x => x != 48 ? Rnd.Range(0, 48) : 48).ToList().Shuffle();
        StartCoroutine(IterateVisual(5));
        Debug.LogFormat("[Identifrac #{0}] Seed is {1}.", _moduleID, seed.Select(x => x == 48 ? "EMPTY" : "" + (x / 24) + ((x / 12) % 2) + ((x / 6) % 2) + ((x / 3) % 2) + (x % 3)).Join(", "));
        Debug.LogFormat("[Identifrac #{0}] Expecting {1}.", _moduleID, seed.Select(x => "" + (x / 7) + (x % 7)).Join(", "));
    }

    void Update()
    {
        if (!solved)
        {
            for (int i = 0; i < ButtonMesh.Count(); i++)
            {
                if (input.Count() - (in2 ? 1 : 0) > i)
                    ButtonMesh[i].material.color = new Color(1, 1, 1);
                else
                    ButtonMesh[i].material.color = new Color(0, 0, 0);
                if (buttoncol[i])
                    ButtonMesh[i].material.color = new Color(ButtonMesh[i].material.color.r * .5f + .25f, ButtonMesh[i].material.color.g * .5f + .25f, ButtonMesh[i].material.color.b * .5f + .25f);
            }
        }
    }

    private IEnumerator IterateVisual(int steps)
    {
        layers = new List<List<GameObject>> { };
        for (int i = 0; i < steps; i++)
        {
            layers.Add(new List<GameObject> { });
            depth *= 2;
            int l = Tiles.Count();
            for (int j = skipme; j < l; j++)
            {
                Tiles[j].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 0);
                for (int k = 0; k < 8; k++)
                {
                    if (seed[type[j][k]] != 48)
                    {
                        type.Add(Modify(Adapt(seed[type[j][k]]), type[j]));
                        Tiles.Add(Instantiate(Tiles.Last(), Tiles[j].transform));
                        layers.Last().Add(Tiles.Last());
                        Tiles.Last().transform.localEulerAngles = new Vector3(0, 0, 0);
                        Tiles.Last().transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                        Tiles.Last().transform.localPosition = new Vector3((k % 2) / 2f - 0.25f, ((k / 2) % 2) / 2f - 0.25f, (k / 4) / 2f - 0.25f);
                        coords.Add(new List<int> { 2 * coords[j][0] + k % 2, 2 * coords[j][1] + (k / 2) % 2, 2 * coords[j][2] + k / 4 });
                        Tiles.Last().GetComponent<MeshRenderer>().material.color = new Color(coords.Last()[0] / (float)(depth - 1), coords.Last()[1] / (float)(depth - 1), coords.Last()[2] / (float)(depth - 1), 1);
                    }
                }
                skipme++;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private bool IsSymmetric(int steps)
    {
        List<int>[,,] grid = new List<int>[1, 1, 1] { { { new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 } } } };
        for (int i = 0; i < steps; i++)
        {
            Debug.Log(2 << i);
            //string output = "";
            List<int>[,,] newGrid = new List<int>[2 << i, 2 << i, 2 << i];
            for (int j = 0; j < (1 << (3 * i)); j++)
            {
                if (grid[j % (1 << i), (j / (1 << i)) % (1 << i), j / (1 << (2 * i))] == null)
                {
                    //output += "-";
                    continue;
                }
                //output += "#";
                for (int k = 0; k < 8; k++)
                {
                    List<int> trCurrent = grid[j % (1 << i), (j / (1 << i)) % (1 << i), j / (1 << (2 * i))];
                    int transformation = seed[trCurrent[k]];
                    if (transformation != 48)
                    {
                        List<int> newValue = Modify(Adapt(transformation), trCurrent);
                        newGrid[2 * (j % (1 << i)) + k % 2, 2 * ((j / (1 << i)) % (1 << i)) + (k / 2) % 2, 2 * (j / (1 << (2 * i))) + k / 4] = newValue;
                    } 
                }
            }
            //Debug.Log(output);
            grid = newGrid;
        }

        for (int i = 1; i < 48; i++)
        {
            bool isSafe = false;
            List<int> currentTransform = Adapt(i);
            for (int j = 0; j < (1 << (3 * steps)); j++)
            {
                List<int> currentPos = new List<int> { };
                List<int> alternatePos = new List<int> { };
                int j2 = j;
                for (int k = 0; k < steps; k++)
                {
                    currentPos.Add(j2 % 8);
                    alternatePos.Add(currentTransform[j2 % 8]);
                    j2 /= 8;
                }

                int x1 = 0, y1 = 0, z1 = 0, x2 = 0, y2 = 0, z2 = 0;
                for (int k = 0; k < currentPos.Count; k++)
                {
                    x1 = x1 * 2 + currentPos[k] % 2;
                    x2 = x2 * 2 + alternatePos[k] % 2;

                    y1 = y1 * 2 + (currentPos[k] / 2) % 2;
                    y2 = y2 * 2 + (alternatePos[k] / 2) % 2;

                    z1 = z1 * 2 + currentPos[k] / 4;
                    z2 = z2 * 2 + alternatePos[k] / 4;
                }

                if ((grid[x1, y1, z1] == null) != (grid[x2, y2, z2] == null))
                {
                    isSafe = true;
                    break;
                }
            }

            if (!isSafe)
                return true;
        }
        return false;
    }

    private List<int> Adapt(int config)
    {
        List<int> L = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
        for (int i = 0; i < config % 3; i++)
            L = Modify(order[4], L);
        if ((config / 3) % 2 == 1)
            L = Modify(order[3], L);
        if ((config / 6) % 2 == 1)
            L = Modify(order[2], L);
        if ((config / 12) % 2 == 1)
            L = Modify(order[1], L);
        if (config / 24 == 1)
            L = Modify(order[0], L);
        return L;
    }

    private List<int> Modify(List<int> transposer, List<int> subject)
    {
        return subject.Select(x => transposer[x]).ToList();
    }

    private IEnumerator BackIterateVisual()
    {
        while (layers.Count > 0)
        {
            float r = 0;
            float g = 0;
            float b = 0;
            float a = 0;
            int n = 0;
            int children = seed.Count(x => x != 48);
            while(layers.Last().Count != 0)
            {
                GameObject item = layers.Last().First();
                layers.Last().RemoveAt(0);
                Color c = item.GetComponent<MeshRenderer>().material.color;
                r += c.r / children;
                g += c.g / children;
                b += c.b / children;
                a += c.a / children;
                n++;
                if (n == children)
                {
                    item.transform.parent.GetComponent<MeshRenderer>().material.color = new Color(r, g, b, a * children / 8);
                    n = 0;
                    r = 0;
                    g = 0;
                    b = 0;
                    a = 0;
                }
                GameObject.DestroyImmediate(item);
            }
            layers.Remove(layers.Last());
            yield return new WaitForSeconds(0.5f);
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} 0123456' to press those buttons respectively. '!{0} rotate' to cycle through all angles.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;
        command = command.ToLowerInvariant();
        if (command == "rotate")
        {
            List<int[]> pos = new List<int[]> { new int[] { 0, 0, 90 }, new int[] { 0, 0, 180 }, new int[] { 0, 0, 270 }, new int[] { 0, 0, 360 }, new int[] { -90, 0, 0 }, new int[] { 360, 0, 0 }, new int[] { 90, 0, 0 }, new int[] { 0, 0, 0 } };
            for (int i = 0; i < pos.Count(); i++)
            {
                Vector3 vector = Tiles[0].transform.localEulerAngles;
                for (float t = 0; t < 1; t += Time.deltaTime * 2)
                {
                    Tiles[0].transform.localEulerAngles = Vector3.Lerp(vector, new Vector3(pos[i][0], pos[i][1], pos[i][2]), t);
                    yield return null;
                }
                Tiles[0].transform.localEulerAngles = new Vector3(pos[i][0], pos[i][1], pos[i][2]);
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            for (int i = 0; i < command.Length; i++)
            {
                if (!"0123456".Contains(command[i]))
                {
                    yield return "sendtochaterror Invalid command.";
                    yield break;
                }
            }
            for (int i = 0; i < command.Length; i++)
            {
                Buttons[command[i] - '0'].OnInteract();
                yield return null;
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        input = new List<int> { };
        for (int i = 0; i < 8; i++)
        {
            Buttons[seed[i] / 7].OnInteract();
            yield return true;
            Buttons[seed[i] % 7].OnInteract();
            yield return true;
        }
    }
}