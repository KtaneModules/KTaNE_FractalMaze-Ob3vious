using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class fractalMazeScript : MonoBehaviour
{

	//public stuff
	public KMAudio Audio;
	public KMSelectable[] Buttons;
	public List<GameObject> Tiles;
	public List<MeshRenderer> ButtonMesh;
	public KMBombModule Module;

	//functionality
	private bool solved = false;
	private int stage = 0;
	private List<int> type = new List<int> { 0 };
	private List<int> seed = new List<int> { 0, 0, 0, 8 };
	private List<int[,]> grid = new List<int[,]> { new int[,] { { 0 } } };
	private List<int[,]> solvematrix = new List<int[,]> { new int[,] { { 0 } } };
	private List<bool[]> buttoncol = new List<bool[]> { }; //higlight, start, end
	private int[,] coords = new int[2, 2];
	int skipme = 0;

	//modifierAdd[old,add]
	private readonly int[,] modifierAdd = new int[,] {
		{ 0, 1, 2, 3, 4, 5, 6, 7 },
		{ 1, 6, 5, 2, 3, 4, 7, 0 },
		{ 2, 3, 0, 1, 6, 7, 4, 5 },
		{ 3, 4, 7, 0, 1, 6, 5, 2 },
		{ 4, 5, 6, 7, 0, 1, 2, 3 },
		{ 5, 2, 1, 6, 7, 0, 3, 4 },
		{ 6, 7, 4, 5, 2, 3, 0, 1 },
		{ 7, 0, 3, 4, 5, 2, 1, 6 }
	};

	//logging
	static int _moduleIdCounter = 1;
	int _moduleID = 0;

	private KMSelectable.OnInteractHandler Press(int pos)
	{
		return delegate
		{
			if (!solved)
			{
				Buttons[pos].AddInteractionPunch(1f);
				int n = 2;
				for (int i = 0; i < stage; i++)
				{
					n *= 2;
				}
				switch (pos)
				{
					case 0:
						if (coords[0, 1] > 0 && grid[stage + 1][coords[0, 1] - 1, coords[0, 0]] != 8)
						{
							coords[0, 1]--;
						}
						else
						{
							Module.HandleStrike();
						}
						break;
					case 1:
						if (coords[0, 0] < n - 1 && grid[stage + 1][coords[0, 1], coords[0, 0] + 1] != 8)
						{
							coords[0, 0]++;
						}
						else
						{
							Module.HandleStrike();
						}
						break;
					case 2:
						if (coords[0, 0] > 0 && grid[stage + 1][coords[0, 1], coords[0, 0] - 1] != 8)
						{
							coords[0, 0]--;
						}
						else
						{
							Module.HandleStrike();
						}
						break;
					case 3:
						if (coords[0, 1] < n - 1 && grid[stage + 1][coords[0, 1] + 1, coords[0, 0]] != 8)
						{
							coords[0, 1]++;
						}
						else
						{
							Module.HandleStrike();
						}
						break;
				}
				if (coords[0, 0] == coords[1, 0] && coords[0, 1] == coords[1, 1])
				{
					Audio.PlaySoundAtTransform("Beep2", Buttons[pos].transform);
					stage--;
					if (stage == -1)
					{
						Module.HandlePass();
						solved = true;
						StartCoroutine(IterateVisual(5));
					}
					else
					{
						Generate();
					}
				}
                else
                {
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
			buttoncol.Add(new bool[3]);
			Buttons[i].OnInteract += Press(i);
			int x = i;
			Buttons[i].OnHighlight += delegate { if (!solved) { buttoncol[x][0] = true; } };
			Buttons[i].OnHighlightEnded += delegate { if (!solved) { buttoncol[x][0] = false; } };
			ButtonMesh.Add(Buttons[i].GetComponent<MeshRenderer>());
		}
	}

	void Start()
	{
		//setting up i0
		seed = new List<int> { Rnd.Range(0, 8), Rnd.Range(0, 8), Rnd.Range(0, 8), 8 }.Shuffle();
		Debug.LogFormat("[Fractal Maze #{0}] Starting seed is {1}.", _moduleID, seed.Select(x => "KBGCRMYWX"[x]).Join(""));
		IterateGrid();
		for (int i = 0; i < 3; i++)
		{
			IterateGrid();
			stage++;
		}
		StartCoroutine(IterateVisual(1));
		Generate();
		StartCoroutine(DoFlashes());
	}

	void Update()
	{
		for (int i = 0; i < ButtonMesh.Count(); i++)
		{
			ButtonMesh[i].material.color = new Color(buttoncol[i][1] ? 1 : 0, buttoncol[i][2] ? 1 : 0, 0);
			if (buttoncol[i][0])
			{
				ButtonMesh[i].material.color = new Color(ButtonMesh[i].material.color.r * .75f + .25f, ButtonMesh[i].material.color.g * .75f + .25f, ButtonMesh[i].material.color.b * .75f + .25f);
			}
		}
	}

	private void Generate()
	{
		int n = 3;
		for (int i = 0; i < stage; i++)
		{
			n *= 3;
		}
		int c = 0;
		if (stage + 1 != solvematrix.Count())
		{
			int n2 = 2;
			for (int i = 0; i < stage; i++)
			{
				n2 *= 2;
			}
			int x = 0;
			for (int i = 0; i < n2; i++)
			{
				for (int j = 0; j < n2; j++)
				{
					if (grid[stage + 1][i, j] != 8)
					{
						if (i == coords[0, 1] / 2 && j == coords[0, 0] / 2)
						{
							c = x;
						}
						x++;
					}
				}
			}
		}
		int a = 0, b = 0;
		while (solvematrix[stage + 1][b, a] == 0)
		{
			if (stage == solvematrix.Count())
				a = Rnd.Range(0, n);
			else
				a = c;
			b = Rnd.Range(0, n);
		}
		n = 2;
		for (int i = 0; i < stage; i++)
		{
			n *= 2;
		}
		int k = 0;
		for (int i = 0; i < n; i++)
		{
			for (int j = 0; j < n; j++)
			{
				if (grid[stage + 1][i, j] != 8)
				{
					if (a == k)
					{
						coords[0, 1] = i;
						coords[0, 0] = j;
					}
					if (b == k)
					{
						coords[1, 1] = i;
						coords[1, 0] = j;
					}
					k++;
				}
			}
		}
		Debug.LogFormat("[Fractal Maze #{0}] (Top left = 1,1) Starting at [{1}], and ending at [{2}].", _moduleID, (coords[0, 0] + 1) + ", " + (coords[0, 1] + 1), (coords[1, 0] + 1) + ", " + (coords[1, 1] + 1));
	}

	private IEnumerator DoFlashes()
	{
		yield return null;
		while (!solved)
		{
			int n = 1;
			for (int i = 0; i < stage; i++)
			{
				n *= 2;
			}
			int x1 = coords[0, 0];
			int y1 = coords[0, 1];
			int x2 = coords[1, 0];
			int y2 = coords[1, 1];
			for (int i = 0; i < stage + 1; i++)
			{
				buttoncol[2][1] = (x1 / n) % 2 == 0;
				buttoncol[1][1] = (x1 / n) % 2 == 1;
				buttoncol[2][2] = (x2 / n) % 2 == 0;
				buttoncol[1][2] = (x2 / n) % 2 == 1;
				buttoncol[0][1] = (y1 / n) % 2 == 0;
				buttoncol[3][1] = (y1 / n) % 2 == 1;
				buttoncol[0][2] = (y2 / n) % 2 == 0;
				buttoncol[3][2] = (y2 / n) % 2 == 1;
				n /= 2;
				yield return new WaitForSeconds(1f);
				for (int j = 0; j < 4; j++)
				{
					buttoncol[j][1] = false;
					buttoncol[j][2] = false;
				}
				yield return new WaitForSeconds(0.5f);
			}
			yield return new WaitForSeconds(1f);
		}
		for (int j = 0; j < 4; j++)
		{
			buttoncol[j][0] = false;
			buttoncol[j][1] = false;
			buttoncol[j][2] = true;
		}
	}

	private IEnumerator IterateVisual(int steps)
	{
		for (int i = 0; i < steps; i++)
		{
			int l = Tiles.Count();
			for (int j = skipme; j < l; j++)
			{
				for (int k = 0; k < 4; k++)
				{
					int[,] order = new int[,] { { 0, 1, 2, 3 }, { 2, 0, 3, 1 }, { 1, 0, 3, 2 }, { 3, 1, 2, 0 }, { 2, 3, 0, 1 }, { 0, 2, 1, 3 }, { 3, 2, 1, 0 }, { 1, 3, 0, 2 } };
					if (seed[order[type[j], k]] != 8)
					{
						type.Add(modifierAdd[seed[order[type[j], k]], type[j]]);
						Tiles.Add(Instantiate(Tiles.Last(), Tiles[j].transform));
						Tiles.Last().transform.localEulerAngles = new Vector3(0, 0, 0);
						Tiles.Last().transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
						Tiles.Last().transform.localPosition = new Vector3((k % 2) / 2f - 0.25f, (k / 2) / -2f + 0.25f, 0f);
						Tiles.Last().GetComponent<MeshRenderer>().material.color = new Color(seed[order[type[j], k]] / 4, (seed[order[type[j], k]] / 2) % 2, seed[order[type[j], k]] % 2);
					}
				}
				Tiles[j].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0, 0);
				skipme++;
			}
			yield return new WaitForSeconds(0.5f);
		}
	}

	private void IterateGrid()
	{
		int l = 1;
		for (int i = 0; i < grid.Count() - 1; i++)
		{
			l *= 2;
		}
		int[,] newgrid = new int[l * 2, l * 2];
		for (int i = 0; i < l; i++)
		{
			for (int j = 0; j < l; j++)
			{
				if (grid.Last()[i, j] != 8)
				{
					for (int k = 0; k < 4; k++)
					{
						int[,] order = new int[,] { { 0, 1, 2, 3 }, { 2, 0, 3, 1 }, { 1, 0, 3, 2 }, { 3, 1, 2, 0 }, { 2, 3, 0, 1 }, { 0, 2, 1, 3 }, { 3, 2, 1, 0 }, { 1, 3, 0, 2 } };
						if (seed[order[grid.Last()[i, j], k]] != 8)
						{
							newgrid[i * 2 + k / 2, j * 2 + k % 2] = modifierAdd[seed[order[grid.Last()[i, j], k]], grid.Last()[i, j]];
						}
						else
						{
							newgrid[i * 2 + k / 2, j * 2 + k % 2] = 8;
						}
					}
				}
				else
				{
					for (int k = 0; k < 4; k++)
					{
						newgrid[i * 2 + k / 2, j * 2 + k % 2] = 8;
					}
				}
			}
		}
		grid.Add(newgrid);
		l *= 2;
		string thing = "";
		for (int i = 0; i < l; i++)
		{
			thing += "\n[Fractal Maze #" + _moduleID + "]";
			for (int j = 0; j < l; j++)
			{
				thing += grid.Last()[i, j] != 8 ? "▓" : "░";
			}
		}
		Debug.LogFormat("[Fractal Maze #{0}] Grid on stage {1} is: {2}", _moduleID, 6 - grid.Count(), thing);
		int n = 0;
		for (int i = 0; i < l; i++)
		{
			for (int j = 0; j < l; j++)
			{
				if (grid.Last()[i, j] != 8)
				{
					n++;
				}
			}
		}
		solvematrix.Add(new int[n, n]);
		int[,] matrixbk = new int[n, n];
		n = 0;
		for (int i = 0; i < l; i++)
		{
			for (int j = 0; j < l; j++)
			{
				if (grid.Last()[i, j] != 8)
				{
					int o = 0;
					for (int k = 0; k < l; k++)
					{
						for (int m = 0; m < l; m++)
						{
							if (grid.Last()[k, m] != 8)
							{
								if (Math.Abs(i - k) + Math.Abs(j - m) == 1)
								{
									if (i - k == 1)
									{
										solvematrix.Last()[o, n] = 1;
									}
									if (m - j == 1)
									{
										solvematrix.Last()[o, n] = 2;
									}
									if (j - m == 1)
									{
										solvematrix.Last()[o, n] = 3;
									}
									if (k - i == 1)
									{
										solvematrix.Last()[o, n] = 4;
									}
								}
								o++;
							}
						}
					}
					n++;
				}
			}
		}
		bool stillgoing = true;
		while (stillgoing)
		{
			stillgoing = false;
			matrixbk = new int[n, n];
			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < n; j++)
				{
					for (int k = 0; k < n; k++)
					{
						if (solvematrix.Last()[j, i] != 0 && solvematrix.Last()[k, j] != 0 && solvematrix.Last()[k, i] == 0)
						{
							matrixbk[k, i] = solvematrix.Last()[j, i];
							stillgoing = true;
						}
					}
				}
			}
			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < n; j++)
				{
					if (solvematrix.Last()[i, j] == 0)
					{
						solvematrix.Last()[i, j] = matrixbk[i, j];
					}
				}
			}
		}
		for (int i = 0; i < n; i++)
		{
			solvematrix.Last()[i, i] = 0;
		}
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} udlr' to press those directions respectively.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		command = command.ToLowerInvariant();
		for (int i = 0; i < command.Length; i++)
		{
			if (!"urld".Contains(command[i]))
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
		}
		for (int i = 0; i < command.Length; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				if ("urld"[j] == command[i])
				{
					Buttons[j].OnInteract();
					yield return null;
				}
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		while (!solved)
		{
			int a = 0, b = 0;
			int n = 2;
			for (int i = 0; i < stage; i++)
			{
				n *= 2;
			}
			int k = 0;
			for (int i = 0; i < n; i++)
			{
				for (int j = 0; j < n; j++)
				{
					if (grid[stage + 1][i, j] != 8)
					{
						if (i == coords[0, 1] && j == coords[0, 0])
						{
							a = k;
						}
						if (i == coords[1, 1] && j == coords[1, 0])
						{
							b = k;
						}
						k++;
					}
				}
			}
			Buttons[solvematrix[stage + 1][b, a] - 1].OnInteract();
			yield return true;
		}
	}
}
