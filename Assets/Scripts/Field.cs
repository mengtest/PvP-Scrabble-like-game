﻿using Mono.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class Field : MonoBehaviour
{
    public enum Direction
    {
        Horizontal, Vertical, None
    }

    #region Prefabs and materials

    public Tile TilePrefab;
    public Material StandardMaterial;
    public Material StartMaterial;
    public Material WordX2Material;
    public Material WordX3Material;
    public Material LetterX2Material;
    public Material LetterX3Material;

    #endregion Prefabs and materials

    public GameObject TimerImage;
    public Text TimerText;
    public Text Player1Text;
    public Text Player2Text;
    public GameObject EndGameCanvas;
    public UIController Controller;
    public Button SkipTurnButton;

    public Direction CurrentDirection = Direction.None;
    public int CurrentTurn = 1;
    public bool isFirstTurn = true;
    public byte NumberOfRows = 15;
    public byte NumberOfColumns = 15;
    public LetterBox Player1;
    public LetterBox Player2;
    public byte CurrentPlayer = 1;
    public float DistanceBetweenTiles = 1.2f;
    public Tile[,] GameField;
    public List<Tile> CurrentTiles;

    private int _turnsSkipped = 0;
    private SqliteConnection _dbConnection;
    private List<Tile> _wordsFound;
    private bool _timerEnabled;
    private int _timerLength;
    private float _timeRemaining;
    private float _xOffset = 0;
    private float _yOffset = 0;

    private void Start()
    {
        CurrentTiles = new List<Tile>();
        var conection = @"URI=file:" + Application.streamingAssetsPath + @"/words.db";
        _dbConnection = new SqliteConnection(conection);
        _dbConnection.Open();
        _wordsFound = new List<Tile>();
        _timerEnabled = PlayerPrefs.GetInt("TimerEnabled") == 1;
        if (_timerEnabled)
        {
            TimerImage.SetActive(true);
            _timerLength = PlayerPrefs.GetInt("Length");
            _timeRemaining = (float)_timerLength + 1;
        }
        var size = gameObject.GetComponent<RectTransform>().rect;
        DistanceBetweenTiles = Math.Min(Math.Abs(size.width * gameObject.transform.lossyScale.x), Math.Abs(size.height * gameObject.transform.lossyScale.y)) / 15; // gameObject.transform.parent.GetComponent<Canvas>().scaleFactor;
        TilePrefab.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(DistanceBetweenTiles, DistanceBetweenTiles);
        Player1.LetterSize = new Vector2(DistanceBetweenTiles, DistanceBetweenTiles);
        Player2.LetterSize = new Vector2(DistanceBetweenTiles, DistanceBetweenTiles);
        _xOffset = (size.width * gameObject.transform.lossyScale.x - DistanceBetweenTiles * 15 + DistanceBetweenTiles / 2) / 2;
        _yOffset = (size.height * gameObject.transform.lossyScale.y - DistanceBetweenTiles * 15 + DistanceBetweenTiles / 2) / 2;
        CreateField();
        Player1Text.text = PlayerPrefs.GetString("Player1", "Гравець 1");
        Player2Text.text = PlayerPrefs.GetString("Player2", "Гравець 2");
    }

    private void Update()
    {
        if (SkipTurnButton.interactable != (CurrentTiles.Count == 0))
            SkipTurnButton.interactable = CurrentTiles.Count == 0;
        if (Input.GetKeyDown(KeyCode.A))
            EndGame(null);
        if (_timerEnabled)
        {
            _timeRemaining -= Time.deltaTime;
            var timerValue = (int)_timeRemaining - 1;
            if (timerValue < 0)
                timerValue = 0;
            TimerText.text = timerValue.ToString();
            if (_timeRemaining < 0)
                OnEndTimer();
        }
    }

    private void CreateField()
    {
        var xOffset = _xOffset;
        var yOffset = _yOffset;
        GameField = new Tile[NumberOfRows, NumberOfColumns];
        for (var i = 0; i < NumberOfRows; i++)
        {
            for (var j = 0; j < NumberOfColumns; j++)
            {
                var newTile = Instantiate(TilePrefab,
                    new Vector2(transform.position.x + xOffset, transform.position.y + yOffset),
                    transform.rotation) as Tile;
                newTile.transform.SetParent(gameObject.transform);
                newTile.Column = j;
                var render = newTile.GetComponent<Image>();
                render.material = StandardMaterial;
                newTile.Row = i;
                GameField[i, j] = newTile;
                xOffset += DistanceBetweenTiles;
            }
            xOffset = _xOffset;
            yOffset += DistanceBetweenTiles;
        }
        GameField[7, 7].CanDrop = true;
        GameField[7, 7].GetComponent<Image>().material = StartMaterial;
        AssignMaterials();
        AssignMultipliers();
    }

    #region GameField generation

    private void AssignMaterials()
    {
        GameField[0, 0].GetComponent<Image>().material = WordX3Material;
        GameField[0, 14].GetComponent<Image>().material = WordX3Material;
        GameField[14, 0].GetComponent<Image>().material = WordX3Material;
        GameField[14, 14].GetComponent<Image>().material = WordX3Material;
        GameField[0, 7].GetComponent<Image>().material = WordX3Material;
        GameField[14, 7].GetComponent<Image>().material = WordX3Material;
        GameField[7, 0].GetComponent<Image>().material = WordX3Material;
        GameField[7, 14].GetComponent<Image>().material = WordX3Material;
        for (var i = 1; i < 5; i++)
        {
            GameField[i, i].GetComponent<Image>().material = WordX2Material;
            GameField[i, NumberOfRows - i - 1].GetComponent<Image>().material = WordX2Material;
            GameField[NumberOfRows - i - 1, i].GetComponent<Image>().material = WordX2Material;
            GameField[NumberOfRows - i - 1, NumberOfRows - i - 1].GetComponent<Image>().material = WordX2Material;
        }
        GameField[5, 1].GetComponent<Image>().material = LetterX3Material;
        GameField[5, 5].GetComponent<Image>().material = LetterX3Material;
        GameField[5, 9].GetComponent<Image>().material = LetterX3Material;
        GameField[5, 13].GetComponent<Image>().material = LetterX3Material;
        GameField[9, 1].GetComponent<Image>().material = LetterX3Material;
        GameField[9, 5].GetComponent<Image>().material = LetterX3Material;
        GameField[9, 9].GetComponent<Image>().material = LetterX3Material;
        GameField[9, 13].GetComponent<Image>().material = LetterX3Material;
        GameField[1, 5].GetComponent<Image>().material = LetterX3Material;
        GameField[1, 9].GetComponent<Image>().material = LetterX3Material;
        GameField[13, 5].GetComponent<Image>().material = LetterX3Material;
        GameField[13, 9].GetComponent<Image>().material = LetterX3Material;
        GameField[0, 3].GetComponent<Image>().material = LetterX2Material;
        GameField[0, 11].GetComponent<Image>().material = LetterX2Material;
        GameField[14, 3].GetComponent<Image>().material = LetterX2Material;
        GameField[14, 11].GetComponent<Image>().material = LetterX2Material;
        GameField[2, 6].GetComponent<Image>().material = LetterX2Material;
        GameField[2, 8].GetComponent<Image>().material = LetterX2Material;
        GameField[12, 6].GetComponent<Image>().material = LetterX2Material;
        GameField[12, 8].GetComponent<Image>().material = LetterX2Material;
        GameField[3, 0].GetComponent<Image>().material = LetterX2Material;
        GameField[3, 7].GetComponent<Image>().material = LetterX2Material;
        GameField[3, 14].GetComponent<Image>().material = LetterX2Material;
        GameField[11, 0].GetComponent<Image>().material = LetterX2Material;
        GameField[11, 7].GetComponent<Image>().material = LetterX2Material;
        GameField[11, 14].GetComponent<Image>().material = LetterX2Material;
        GameField[6, 2].GetComponent<Image>().material = LetterX2Material;
        GameField[6, 6].GetComponent<Image>().material = LetterX2Material;
        GameField[6, 8].GetComponent<Image>().material = LetterX2Material;
        GameField[6, 12].GetComponent<Image>().material = LetterX2Material;
        GameField[8, 2].GetComponent<Image>().material = LetterX2Material;
        GameField[8, 6].GetComponent<Image>().material = LetterX2Material;
        GameField[8, 8].GetComponent<Image>().material = LetterX2Material;
        GameField[8, 12].GetComponent<Image>().material = LetterX2Material;
        GameField[7, 3].GetComponent<Image>().material = LetterX2Material;
        GameField[7, 11].GetComponent<Image>().material = LetterX2Material;
    }

    private void AssignMultipliers()
    {
        GameField[0, 0].WordMultiplier = 3;
        GameField[0, 14].WordMultiplier = 3;
        GameField[14, 0].WordMultiplier = 3;
        GameField[14, 14].WordMultiplier = 3;
        GameField[0, 7].WordMultiplier = 3;
        GameField[14, 7].WordMultiplier = 3;
        GameField[7, 0].WordMultiplier = 3;
        GameField[7, 14].WordMultiplier = 3;
        for (var i = 1; i < 5; i++)
        {
            GameField[i, i].WordMultiplier = 2;
            GameField[i, NumberOfRows - i - 1].WordMultiplier = 2;
            GameField[NumberOfRows - i - 1, i].WordMultiplier = 2;
            GameField[NumberOfRows - i - 1, NumberOfRows - i - 1].WordMultiplier = 2;
        }
        GameField[5, 1].LetterMultiplier = 3;
        GameField[5, 5].LetterMultiplier = 3;
        GameField[5, 9].LetterMultiplier = 3;
        GameField[5, 13].LetterMultiplier = 3;
        GameField[9, 1].LetterMultiplier = 3;
        GameField[9, 5].LetterMultiplier = 3;
        GameField[9, 9].LetterMultiplier = 3;
        GameField[9, 13].LetterMultiplier = 3;
        GameField[1, 5].LetterMultiplier = 3;
        GameField[1, 9].LetterMultiplier = 3;
        GameField[13, 5].LetterMultiplier = 3;
        GameField[13, 9].LetterMultiplier = 3;
        GameField[0, 3].LetterMultiplier = 2;
        GameField[0, 11].LetterMultiplier = 2;
        GameField[14, 3].LetterMultiplier = 2;
        GameField[14, 11].LetterMultiplier = 2;
        GameField[2, 6].LetterMultiplier = 2;
        GameField[2, 8].LetterMultiplier = 2;
        GameField[12, 6].LetterMultiplier = 2;
        GameField[12, 8].LetterMultiplier = 2;
        GameField[3, 0].LetterMultiplier = 2;
        GameField[3, 7].LetterMultiplier = 2;
        GameField[3, 14].LetterMultiplier = 2;
        GameField[11, 0].LetterMultiplier = 2;
        GameField[11, 7].LetterMultiplier = 2;
        GameField[11, 14].LetterMultiplier = 2;
        GameField[6, 2].LetterMultiplier = 2;
        GameField[6, 6].LetterMultiplier = 2;
        GameField[6, 8].LetterMultiplier = 2;
        GameField[6, 12].LetterMultiplier = 2;
        GameField[8, 2].LetterMultiplier = 2;
        GameField[8, 6].LetterMultiplier = 2;
        GameField[8, 8].LetterMultiplier = 2;
        GameField[8, 12].LetterMultiplier = 2;
        GameField[7, 3].LetterMultiplier = 2;
        GameField[7, 11].LetterMultiplier = 2;
    }

    #endregion GameField generation

    private void OnEndTimer()
    {
        _timeRemaining = (float)_timerLength + 1;
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
                if (CurrentPlayer == 1)
                {
                    Player1.ChangeBox(7 - Player1.CurrentLetters.Count);
                    Player1.Score += points;
                    if (Player1.CurrentLetters.Count == 0)
                    {
                        EndGame(Player1);
                    }
                    Player1.gameObject.SetActive(false);
                    Player2.gameObject.SetActive(true);
                    CurrentTiles.Clear();
                    CurrentDirection = Direction.None;
                    CurrentPlayer = 2;
                    Controller.InvalidatePlayer(1, Player1.Score);
                    isFirstTurn = false;
                }
                else
                {
                    Player2.ChangeBox(7 - Player2.CurrentLetters.Count);
                    Player2.Score += points;
                    if (Player2.CurrentLetters.Count == 0)
                        EndGame(Player2);
                    Player1.gameObject.SetActive(true);
                    Player2.gameObject.SetActive(false);
                    CurrentDirection = Direction.None;
                    CurrentTiles.Clear();
                    CurrentPlayer = 1;
                    Controller.InvalidatePlayer(2, Player2.Score);
                    isFirstTurn = false;
                }
                if (_timerEnabled)
                    _timeRemaining = (float)_timerLength + 1;
            }
            else Controller.ShowNotExistError();
        }
        else Controller.ShowZeroTilesError();
        _wordsFound = new List<Tile>();
    }

    public void OnChangeLetters()
    {
        if (CurrentPlayer == 1)
        {
            if (!Player1.ChangeLetters())
            {
                Controller.ShowChangeLetterError();
                return;
            }
            _turnsSkipped = 0;
            Player1.gameObject.SetActive(false);
            Player2.gameObject.SetActive(true);
            CurrentPlayer = 2;
            Controller.InvalidatePlayer(1, Player1.Score);
            CurrentTiles.Clear();
        }
        else
        {
            if (!Player2.ChangeLetters())
            {
                Controller.ShowChangeLetterError();
                return;
            }
            _turnsSkipped = 0;
            Player1.gameObject.SetActive(true);
            Player2.gameObject.SetActive(false);
            CurrentPlayer = 1;
            Controller.InvalidatePlayer(2, Player2.Score);
            CurrentTiles.Clear();
        }
        if (_timerEnabled)
            _timeRemaining = (float)_timerLength + 1;
    }

    public void OnSkipTurn()
    {
        if (CurrentPlayer == 1)
        {
            Player1.gameObject.SetActive(false);
            Player2.gameObject.SetActive(true);
            CurrentPlayer = 2;
            Controller.InvalidatePlayer(1, Player1.Score);
        }
        else
        {
            Player1.gameObject.SetActive(true);
            Player2.gameObject.SetActive(false);
            CurrentPlayer = 1;
            Controller.InvalidatePlayer(2, Player2.Score);
        }
        if (_timerEnabled)
            _timeRemaining = (float)_timerLength + 1;
        if (++_turnsSkipped == 4)
            EndGame(null);
    }

    public void OnRemoveAll()
    {
        for (var i = CurrentTiles.Count - 1; i >= 0; i--)
        {
            CurrentTiles[i].RemoveTile();
        }
        CurrentTiles.Clear();
    }

    #region Word cheking

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
                    current = CreateWord(Direction.Horizontal, GameField[CurrentTiles[0].Row, currentStart], currentEnd);
                    wordExists = CheckWord(current);
                    if (wordExists)
                    {
                        _wordsFound.Add(GameField[CurrentTiles[0].Row, currentStart]);
                        _wordsFound.Add(GameField[CurrentTiles[0].Row, currentEnd]);
                    }
                    else return false;
                    wordFound = true;
                }
                FindWord(CurrentTiles[0], Direction.Vertical, out currentStart, out currentEnd);
                if (currentStart != currentEnd)
                {
                    current = CreateWord(Direction.Vertical, GameField[currentStart, CurrentTiles[0].Column], currentEnd);
                    wordExists = CheckWord(current);
                    if (wordExists)
                    {
                        _wordsFound.Add(GameField[currentStart, CurrentTiles[0].Column]);
                        _wordsFound.Add(GameField[currentEnd, CurrentTiles[0].Column]);
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
            current = CreateWord(CurrentDirection, GameField[CurrentTiles[0].Row, currentStart], currentEnd);
            wordExists = CheckWord(current);
            if (wordExists)
            {
                _wordsFound.Add(GameField[CurrentTiles[0].Row, currentStart]);
                _wordsFound.Add(GameField[CurrentTiles[0].Row, currentEnd]);
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
                current = CreateWord(CurrentDirection, GameField[currentStart, tile.Column], currentEnd);
                wordExists = CheckWord(current);
                if (wordExists)
                {
                    _wordsFound.Add(GameField[currentStart, tile.Column]);
                    _wordsFound.Add(GameField[currentEnd, tile.Column]);
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
            current = CreateWord(CurrentDirection, GameField[currentStart, CurrentTiles[0].Column], currentEnd);
            wordExists = CheckWord(current);
            if (wordExists)
            {
                _wordsFound.Add(GameField[currentStart, CurrentTiles[0].Column]);
                _wordsFound.Add(GameField[currentEnd, CurrentTiles[0].Column]);
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
                current = CreateWord(CurrentDirection, GameField[tile.Row, currentStart], currentEnd);
                wordExists = CheckWord(current);
                if (wordExists)
                {
                    _wordsFound.Add(GameField[tile.Row, currentStart]);
                    _wordsFound.Add(GameField[tile.Row, currentEnd]);
                }
                else return false;
            }
        }
        return true;
    }

    private int CountPoints()
    {
        var result = 0;
        var wordMultiplier = 1;
        var score = new int[_wordsFound.Count / 2];
        for (var i = 0; i < _wordsFound.Count; i += 2)
        {
            var tempRes = 0;
            if (_wordsFound[i].Row == _wordsFound[i + 1].Row)
                for (var j = _wordsFound[i].Column; j <= _wordsFound[i + 1].Column; j++)
                {
                    var tile = GameField[_wordsFound[i].Row, j];
                    tempRes += LetterBox.PointsDictionary[tile.CurrentLetter.text] * tile.LetterMultiplier;
                    tile.LetterMultiplier = 1;
                    wordMultiplier *= tile.WordMultiplier;
                    tile.WordMultiplier = 1;
                }
            else
            {
                for (var j = _wordsFound[i].Row; j <= _wordsFound[i + 1].Row; j++)
                {
                    var tile = GameField[j, _wordsFound[i].Column];
                    tempRes += LetterBox.PointsDictionary[tile.CurrentLetter.text] * tile.LetterMultiplier;
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
            GameField[start, 0].SetPoints(i * wordMultiplier);
            start--;
        }
        return result * wordMultiplier;
    }

    private string CreateWord(Direction current, Tile start, int end)
    {
        var sb = new StringBuilder();
        if (current == Direction.Vertical)
        {
            for (int j = end; j >= start.Row; j--)
            {
                string temp = GameField[j, start.Column].CurrentLetter.text;
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
                var temp = GameField[start.Row, j].CurrentLetter.text;
                if (String.Equals("*", temp))
                    temp = "_";
                sb.Append(temp);
            }
            return sb.ToString();
        }
    }

    private void FindWord(Tile currentTile, Direction current, out int startPosition, out int endPosition)
    {
        if (current == Direction.Vertical)
        {
            var j = currentTile.Row;
            while (j >= 0 && GameField[j, currentTile.Column].HasLetter)
            {
                j--;
            }
            j++;
            startPosition = j;
            j = currentTile.Row;
            while (j < NumberOfRows && GameField[j, currentTile.Column].HasLetter)
            {
                j++;
            }
            j--;
            endPosition = j;
        }
        else
        {
            var j = currentTile.Column;
            while (j >= 0 && GameField[currentTile.Row, j].HasLetter)
            {
                j--;
            }
            j++;
            startPosition = j;
            j = currentTile.Column;
            while (j < NumberOfRows && GameField[currentTile.Row, j].HasLetter)
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

    #endregion Word cheking

    private void EndGame(LetterBox playerOut)//Player, who ran out of letters is passed
    {
        var tempPoints = Player1.RemovePoints();
        tempPoints += Player2.RemovePoints();
        if (playerOut != null)
        {
            playerOut.Score += tempPoints;
        }
        var winner = Player1.Score > Player2.Score ? 1 : 2;
        Controller.SetWinner(winner, Player1.Score, Player2.Score, Player1Text.text, Player2Text.text);
    }
}