﻿using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class FieldLAN : MonoBehaviour
{
    public enum Direction
    {
        Horizontal, Vertical, None
    }

    #region Prefabs and materials

    public TileLAN TilePrefab;
    public Material StandardMaterial;
    public Material StartMaterial;
    public Material WordX2Material;
    public Material WordX3Material;
    public Material LetterX2Material;
    public Material LetterX3Material;

    #endregion Prefabs and materials

    public UIGrid FieldGrid;
    public GameObject TimerImage;
    public Text TimerText;
    public Text Player1Text;
    public Text Player2Text;
    public UIController Controller;
    public Direction CurrentDirection = Direction.None;
    public int CurrentTurn = 1;
    public bool IsFirstTurn = true;
    public byte NumberOfRows = 15;
    public byte NumberOfColumns = 15;
    public LetterBoxLAN Player1;
    public LetterBoxLAN PlayerToSendCommands;
    public float DistanceBetweenTiles = 1.2f;
    public TileLAN[,] Field;
    public List<TileLAN> CurrentTiles;
    public int PlayerNumber;

    private int _turnsSkipped = 0;
    private SqliteConnection _dbConnection;
    private List<TileLAN> _wordsFound;
    private bool _fixed;//Required to fix error with materials
    private bool _gameStarted;

    private bool _timerEnabled;
    private int _timerLength;
    public float TimeRemaining;

    private void Start()
    {
        CurrentTiles = new List<TileLAN>();
        var conection = @"URI=file:" + Application.streamingAssetsPath + @"/words.db";
        _dbConnection = new SqliteConnection(conection);
        _dbConnection.Open();
        _wordsFound = new List<TileLAN>();
        FieldGrid.Initialize();
        DistanceBetweenTiles = FieldGrid.Items[0, 0].gameObject.GetComponent<RectTransform>().rect.width;
        CreateField();
        _fixed = true;
    }

    private void Update()
    {
        if (_gameStarted && Player1 == null)
            Controller.ShowConnectionError();
        Controller.SetSkipButtonActive(CurrentTiles.Count == 0);
        if (Input.GetKeyDown(KeyCode.A) && Player1 != null)
        {
            Player1.EndGame();
        }
        if (_timerEnabled)
        {
            TimeRemaining -= Time.deltaTime;
            if (Player1 == null)
                return;
            var value = Player1.isServer ? (int)TimeRemaining : (int)TimeRemaining - 2;
            if (value < 0) value = 0;
            TimerText.text = value.ToString();
            if (TimeRemaining < 0)
                OnEndTimer();
        }
        if (Player1 == null && PlayerToSendCommands != null && GameObject.FindGameObjectsWithTag("Player").Length > 1)//Do not touch. It's a feature
            foreach (var o in GameObject.FindGameObjectsWithTag("Player"))
            {
                if (o.GetComponent<LetterBoxLAN>() == PlayerToSendCommands)
                    continue;
                Player1 = o.GetComponent<LetterBoxLAN>();
                _gameStarted = true;
                break;
            }
        else if (Player1 != null)
        {
            Controller.SetChangeButtonActive(Player1.AllLetters.Count > 0 && Player1.CanChangeLetters);
        }
    }

    private void CreateField()
    {
        Field = new TileLAN[NumberOfRows, NumberOfColumns];
        for (var i = 0; i < NumberOfRows; i++)
        {
            for (var j = 0; j < NumberOfColumns; j++)
            {
                var newTile = Instantiate(TilePrefab) as TileLAN;
                newTile.transform.SetParent(gameObject.transform);
                newTile.Column = j;
                var render = newTile.GetComponent<Image>();
                render.material = StandardMaterial;
                newTile.Row = i;
                Field[i, j] = newTile;
                FieldGrid.AddElement(i, j, newTile.gameObject);
            }
        }
        Field[7, 7].CanDrop = true;
        Field[7, 7].GetComponent<Image>().material = StartMaterial;
        AssignMaterials();
        AssignMultipliers();
    }

    #region Field generation

    private void AssignMaterials()
    {
        Field[0, 0].GetComponent<Image>().material = WordX3Material;
        Field[0, 14].GetComponent<Image>().material = WordX3Material;
        Field[14, 0].GetComponent<Image>().material = WordX3Material;
        Field[14, 14].GetComponent<Image>().material = WordX3Material;
        Field[0, 7].GetComponent<Image>().material = WordX3Material;
        Field[14, 7].GetComponent<Image>().material = WordX3Material;
        Field[7, 0].GetComponent<Image>().material = WordX3Material;
        Field[7, 14].GetComponent<Image>().material = WordX3Material;
        for (var i = 1; i < 5; i++)
        {
            Field[i, i].GetComponent<Image>().material = WordX2Material;
            Field[i, NumberOfRows - i - 1].GetComponent<Image>().material = WordX2Material;
            Field[NumberOfRows - i - 1, i].GetComponent<Image>().material = WordX2Material;
            Field[NumberOfRows - i - 1, NumberOfRows - i - 1].GetComponent<Image>().material = WordX2Material;
        }
        Field[5, 1].GetComponent<Image>().material = LetterX3Material;
        Field[5, 5].GetComponent<Image>().material = LetterX3Material;
        Field[5, 9].GetComponent<Image>().material = LetterX3Material;
        Field[5, 13].GetComponent<Image>().material = LetterX3Material;
        Field[9, 1].GetComponent<Image>().material = LetterX3Material;
        Field[9, 5].GetComponent<Image>().material = LetterX3Material;
        Field[9, 9].GetComponent<Image>().material = LetterX3Material;
        Field[9, 13].GetComponent<Image>().material = LetterX3Material;
        Field[1, 5].GetComponent<Image>().material = LetterX3Material;
        Field[1, 9].GetComponent<Image>().material = LetterX3Material;
        Field[13, 5].GetComponent<Image>().material = LetterX3Material;
        Field[13, 9].GetComponent<Image>().material = LetterX3Material;
        Field[0, 3].GetComponent<Image>().material = LetterX2Material;
        Field[0, 11].GetComponent<Image>().material = LetterX2Material;
        Field[14, 3].GetComponent<Image>().material = LetterX2Material;
        Field[14, 11].GetComponent<Image>().material = LetterX2Material;
        Field[2, 6].GetComponent<Image>().material = LetterX2Material;
        Field[2, 8].GetComponent<Image>().material = LetterX2Material;
        Field[12, 6].GetComponent<Image>().material = LetterX2Material;
        Field[12, 8].GetComponent<Image>().material = LetterX2Material;
        Field[3, 0].GetComponent<Image>().material = LetterX2Material;
        Field[3, 7].GetComponent<Image>().material = LetterX2Material;
        Field[3, 14].GetComponent<Image>().material = LetterX2Material;
        Field[11, 0].GetComponent<Image>().material = LetterX2Material;
        Field[11, 7].GetComponent<Image>().material = LetterX2Material;
        Field[11, 14].GetComponent<Image>().material = LetterX2Material;
        Field[6, 2].GetComponent<Image>().material = LetterX2Material;
        Field[6, 6].GetComponent<Image>().material = LetterX2Material;
        Field[6, 8].GetComponent<Image>().material = LetterX2Material;
        Field[6, 12].GetComponent<Image>().material = LetterX2Material;
        Field[8, 2].GetComponent<Image>().material = LetterX2Material;
        Field[8, 6].GetComponent<Image>().material = LetterX2Material;
        Field[8, 8].GetComponent<Image>().material = LetterX2Material;
        Field[8, 12].GetComponent<Image>().material = LetterX2Material;
        Field[7, 3].GetComponent<Image>().material = LetterX2Material;
        Field[7, 11].GetComponent<Image>().material = LetterX2Material;
    }

    private void AssignMultipliers()
    {
        Field[0, 0].WordMultiplier = 3;
        Field[0, 14].WordMultiplier = 3;
        Field[14, 0].WordMultiplier = 3;
        Field[14, 14].WordMultiplier = 3;
        Field[0, 7].WordMultiplier = 3;
        Field[14, 7].WordMultiplier = 3;
        Field[7, 0].WordMultiplier = 3;
        Field[7, 14].WordMultiplier = 3;
        for (var i = 1; i < 5; i++)
        {
            Field[i, i].WordMultiplier = 2;
            Field[i, NumberOfRows - i - 1].WordMultiplier = 2;
            Field[NumberOfRows - i - 1, i].WordMultiplier = 2;
            Field[NumberOfRows - i - 1, NumberOfRows - i - 1].WordMultiplier = 2;
        }
        Field[5, 1].LetterMultiplier = 3;
        Field[5, 5].LetterMultiplier = 3;
        Field[5, 9].LetterMultiplier = 3;
        Field[5, 13].LetterMultiplier = 3;
        Field[9, 1].LetterMultiplier = 3;
        Field[9, 5].LetterMultiplier = 3;
        Field[9, 9].LetterMultiplier = 3;
        Field[9, 13].LetterMultiplier = 3;
        Field[1, 5].LetterMultiplier = 3;
        Field[1, 9].LetterMultiplier = 3;
        Field[13, 5].LetterMultiplier = 3;
        Field[13, 9].LetterMultiplier = 3;
        Field[0, 3].LetterMultiplier = 2;
        Field[0, 11].LetterMultiplier = 2;
        Field[14, 3].LetterMultiplier = 2;
        Field[14, 11].LetterMultiplier = 2;
        Field[2, 6].LetterMultiplier = 2;
        Field[2, 8].LetterMultiplier = 2;
        Field[12, 6].LetterMultiplier = 2;
        Field[12, 8].LetterMultiplier = 2;
        Field[3, 0].LetterMultiplier = 2;
        Field[3, 7].LetterMultiplier = 2;
        Field[3, 14].LetterMultiplier = 2;
        Field[11, 0].LetterMultiplier = 2;
        Field[11, 7].LetterMultiplier = 2;
        Field[11, 14].LetterMultiplier = 2;
        Field[6, 2].LetterMultiplier = 2;
        Field[6, 6].LetterMultiplier = 2;
        Field[6, 8].LetterMultiplier = 2;
        Field[6, 12].LetterMultiplier = 2;
        Field[8, 2].LetterMultiplier = 2;
        Field[8, 6].LetterMultiplier = 2;
        Field[8, 8].LetterMultiplier = 2;
        Field[8, 12].LetterMultiplier = 2;
        Field[7, 3].LetterMultiplier = 2;
        Field[7, 11].LetterMultiplier = 2;
    }

    #endregion Field generation

    private void OnEndTimer()
    {
        TimeRemaining = (float)_timerLength + 1;
        OnRemoveAll();
        OnSkipTurn();
    }

    public void OnEndTurn()
    {
        if (CurrentTiles.Count > 0)
        {
            if (CheckWords())
            {
                _turnsSkipped = 0;
                CurrentTurn++;
                var points = CountPoints();
                Player1.ChangeBox(7 - Player1.CurrentLetters.Count);
                CurrentTiles = new List<TileLAN>();
                CurrentDirection = Direction.None;
                IsFirstTurn = false;
                if (_timerEnabled)
                    TimeRemaining = (float)_timerLength + 1;
                Player1.ChangePlayer(PlayerNumber == 1 ? 2 : 1, points);
                if (Player1.CurrentLetters.Count == 0)
                {
                    Player1.EndGame();
                }
            }
            else Controller.ShowNotExistError();
        }
        else Controller.ShowZeroTilesError();
        _wordsFound = new List<TileLAN>();
    }

    public void OnChangeLetters()
    {
        if (Player1.ChangeLetters())
            CurrentTurn++;
        else
        {
            Controller.ShowChangeLetterError();
            return;
        }
        _turnsSkipped = 0;
        Player1.ChangePlayer(PlayerNumber == 1 ? 2 : 1, 0);
        if (_timerEnabled)
            TimeRemaining = (float)_timerLength + 1;
    }

    public void OnSkipTurn()
    {
        Player1.ChangePlayer(PlayerNumber == 1 ? 2 : 1, 0);
        if (_timerEnabled)
            TimeRemaining = (float)_timerLength + 1;
        if (++_turnsSkipped == 3)
        {
            Player1.EndGame();
        }
    }

    public void OnRemoveAll()
    {
        var sb = new StringBuilder();
        for (var i = CurrentTiles.Count - 1; i >= 0; i--)
        {
            sb.Append(CurrentTiles[i].Row + " " + CurrentTiles[i].Column + " ");
        }
        if (sb.Length != 0)
            sb.Append(Player1.CurrentPlayer);
        Player1.DeleteOnSkip(sb.ToString().Trim());
    }

    #region Words Checking

    private bool CheckWords()
    {
        switch (CurrentDirection)
        {
            case Direction.None:
                bool wordFound = false;
                int currentStart;
                int currentEnd;
                FindWord(CurrentTiles[0], Direction.Horizontal, out currentStart, out currentEnd);
                string current;
                bool wordExists;
                if (currentStart != currentEnd)
                {
                    current = CreateWord(Direction.Horizontal, Field[CurrentTiles[0].Row, currentStart], currentEnd);
                    wordExists = CheckWord(current);
                    if (wordExists)
                    {
                        _wordsFound.Add(Field[CurrentTiles[0].Row, currentStart]);
                        _wordsFound.Add(Field[CurrentTiles[0].Row, currentEnd]);
                    }
                    else return false;
                    wordFound = true;
                }
                FindWord(CurrentTiles[0], Direction.Vertical, out currentStart, out currentEnd);
                if (currentStart != currentEnd)
                {
                    current = CreateWord(Direction.Vertical, Field[currentStart, CurrentTiles[0].Column], currentEnd);
                    wordExists = CheckWord(current);
                    if (wordExists)
                    {
                        _wordsFound.Add(Field[currentStart, CurrentTiles[0].Column]);
                        _wordsFound.Add(Field[currentEnd, CurrentTiles[0].Column]);
                    }
                    else return false;
                    wordFound = true;
                }
                return wordFound;

            case Direction.Vertical:
                return CheckVertical();

            case Direction.Horizontal:
                return CheckHorizontal();

            default:
                return false;
        }
    }

    private bool CheckHorizontal()
    {
        int currentStart, currentEnd;
        string current;
        bool wordExists;
        FindWord(CurrentTiles[0], CurrentDirection, out currentStart, out currentEnd);
        if (currentStart != currentEnd)
        {
            current = CreateWord(CurrentDirection, Field[CurrentTiles[0].Row, currentStart], currentEnd);
            wordExists = CheckWord(current);
            if (wordExists)
            {
                _wordsFound.Add(Field[CurrentTiles[0].Row, currentStart]);
                _wordsFound.Add(Field[CurrentTiles[0].Row, currentEnd]);
            }
            else return false;
        }
        else return false;
        CurrentDirection = Direction.Vertical;
        foreach (var tile in CurrentTiles)
        {
            FindWord(tile, CurrentDirection, out currentStart, out currentEnd);
            if (currentStart != currentEnd)
            {
                current = CreateWord(CurrentDirection, Field[currentStart, tile.Column], currentEnd);
                wordExists = CheckWord(current);
                if (wordExists)
                {
                    _wordsFound.Add(Field[currentStart, tile.Column]);
                    _wordsFound.Add(Field[currentEnd, tile.Column]);
                }
                else return false;
            }
        }
        return true;
    }

    private bool CheckVertical()
    {
        int currentStart, currentEnd;
        string current;
        bool wordExists;
        FindWord(CurrentTiles[0], CurrentDirection, out currentStart, out currentEnd);
        if (currentStart != currentEnd)
        {
            current = CreateWord(CurrentDirection, Field[currentStart, CurrentTiles[0].Column], currentEnd);
            wordExists = CheckWord(current);
            if (wordExists)
            {
                _wordsFound.Add(Field[currentStart, CurrentTiles[0].Column]);
                _wordsFound.Add(Field[currentEnd, CurrentTiles[0].Column]);
            }
            else return false;
            CurrentDirection = Direction.Horizontal;
        }
        else return false;
        foreach (var tile in CurrentTiles)
        {
            FindWord(tile, CurrentDirection, out currentStart, out currentEnd);
            if (currentStart != currentEnd)
            {
                current = CreateWord(CurrentDirection, Field[tile.Row, currentStart], currentEnd);
                wordExists = CheckWord(current);
                if (wordExists)
                {
                    _wordsFound.Add(Field[tile.Row, currentStart]);
                    _wordsFound.Add(Field[tile.Row, currentEnd]);
                }
                else return false;
            }
        }
        return true;
    }

    private int CountPoints()
    {
        var result = 0;
        var score = new int[_wordsFound.Count / 2];
        var wordMultiplier = 1;
        for (var i = 0; i < _wordsFound.Count; i += 2)
        {
            var tempRes = 0;
            if (_wordsFound[i].Row == _wordsFound[i + 1].Row)
                for (var j = _wordsFound[i].Column; j <= _wordsFound[i + 1].Column; j++)
                {
                    var tile = Field[_wordsFound[i].Row, j];
                    tempRes += LetterBoxH.PointsDictionary[tile.CurrentLetter.text] * tile.LetterMultiplier;
                    tile.LetterMultiplier = 1;
                    wordMultiplier *= tile.WordMultiplier;
                    tile.WordMultiplier = 1;
                }
            else
            {
                for (var j = _wordsFound[i].Row; j <= _wordsFound[i + 1].Row; j++)
                {
                    var tile = Field[j, _wordsFound[i].Column];
                    tempRes += LetterBoxH.PointsDictionary[tile.CurrentLetter.text] * tile.LetterMultiplier;
                    tile.LetterMultiplier = 1;
                    wordMultiplier *= tile.WordMultiplier;
                    tile.WordMultiplier = 1;
                }
            }
            result += tempRes;
            score[i / 2] = tempRes;
        }
        var start = 7 + _wordsFound.Count / 2;
        foreach (var i in score)
        {
            Field[start, 0].SetPoints(i * wordMultiplier);
            start--;
        }
        return result * wordMultiplier;
    }

    private string CreateWord(Direction current, TileLAN start, int end)
    {
        var sb = new StringBuilder();
        if (current == Direction.Vertical)
        {
            for (int j = end; j >= start.Row; j--)
            {
                string temp = Field[j, start.Column].CurrentLetter.text;
                if (String.Equals("*", temp))
                    temp = "_";
                sb.Append(temp);
            }
            return sb.ToString();
        }
        else
        {
            for (int j = start.Column; j <= end; j++)
            {
                var temp = Field[start.Row, j].CurrentLetter.text;
                if (String.Equals("*", temp))
                    temp = "_";
                sb.Append(temp);
            }
            return sb.ToString();
        }
    }

    private void FindWord(TileLAN currentTile, Direction current, out int startPosition, out int endPosition)
    {
        if (current == Direction.Vertical)
        {
            var j = currentTile.Row;
            while (j >= 0 && Field[j, currentTile.Column].HasLetter)
            {
                j--;
            }
            j++;
            startPosition = j;
            j = currentTile.Row;
            while (j < NumberOfRows && Field[j, currentTile.Column].HasLetter)
            {
                j++;
            }
            j--;
            endPosition = j;
        }
        else
        {
            var j = currentTile.Column;
            while (j >= 0 && Field[currentTile.Row, j].HasLetter)
            {
                j--;
            }
            j++;
            startPosition = j;
            j = currentTile.Column;
            while (j < NumberOfRows && Field[currentTile.Row, j].HasLetter)
            {
                j++;
            }
            j--;
            endPosition = j;
        }
    }

    private bool CheckWord(string word)
    {
        var sql = "SELECT count(*) FROM AllWords WHERE Word like \"" + word.ToLower() + "\"";
        var command = new SqliteCommand(sql, _dbConnection);
        var inp = command.ExecuteScalar();
        return Convert.ToInt32(inp) != 0;
        /*if (Convert.ToInt32(inp) != 0)
            return true;
        else
        {
            sql = "SELECT count(*) FROM AllWords WHERE Word like \"" + word.ToLower() + "\"";
            command = new SqliteCommand(sql, _dbConnection);
            inp = command.ExecuteScalar();
            return Convert.ToInt32(inp) != 0;
        }*/
    }

    #endregion Words Checking

    public void EndGame(int winner, int player1Score, int player2Score)//Player, who ran out of letters is passed
    {
        Controller.SetWinner(winner, player1Score, player2Score, Player1Text.text, Player2Text.text);
    }

    public void InvalidatePlayer(int playerNumber, int score)
    {
        Controller.InvalidatePlayer(playerNumber, score, playerNumber == PlayerNumber);
        if (_fixed)
            Controller.FixFirstTurn();
        _fixed = false;
    }

    public void SetTimer(bool enabled, int length = 0)
    {
        _timerEnabled = enabled;
        _timerLength = length;
        if (_timerEnabled)
        {
            TimerImage.SetActive(true);
            TimeRemaining = (float)_timerLength + 1;
        }
    }

    public void SetName(int playerNumber, string name)
    {
        if (playerNumber == 1)
            Player1Text.text = name;
        else Player2Text.text = name;
    }
}