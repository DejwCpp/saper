using System.CodeDom;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace saper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    // Button Tags documentation:
    //
    // "hide"      - default for every button at the beginning
    // "open"      - buttons that have already been exposed
    // "bomb"      - undiscovered bomb
    // "flag"      - placed flag
    // "bomb_flag" - flag placed on undiscovered bomb

    public partial class MainWindow : Window
    {
        // COLORS //
        const string windowBgColor      = "#020122";
        const string hideBtnColor       = "#edd382";
        const string hideBtnHoverColor  = "#fde392";
        const string openBtnColor       = "#f2f3ae";
        const string flagBtnColor       = "#fc9e4f";
        const string flagsLabelColor    = "#f4442e";
        const string timerLabelColor    = "#f4442e";
        const string YouLostLabelColor  = "#f4442e";
        const string YouWonLabelColor   = "#f4442e";
        const string yourScoreColor     = "#f4442e";
        const string bestScoreColor     = "#f4442e";
        const string newRecordColor     = "#16db65";
        const string menuBtnsColor      = "#edd382";
        const string menuBtnsColorHover = "#fde392";
        const string menuBtnsFontColor  = "#020122";
        // the other colors are set statically

        // GLOBAL VARIABLES //
        int globalWidth = 0;
        int globalHeight = 0;
        int globalNumOfBombs = 0;
        int globalFieldsLeft = 0;
        int globalYourScoreConverted = 0;

        string globalLevel = "none";
        string globalBestScore = "999999";

        bool globalShowBestScore = true;

        Grid mainGrid = new Grid();
        Grid headerGrid = new Grid();
        Grid mapGrid = new Grid();

        public MainWindow()
        {
            InitializeComponent();

            WindowAlignCenter();

            DisplayMenu();

            SetWindowBgColor(windowBgColor);

            PreventChangingColorOnHoverInMenuButtons(menuBtnsColor, menuBtnsColorHover);

            SetMenuButtonsColor();

            this.SizeChanged += MainWindow_SizeChanged;
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Updates menu label's and buttons font size in menu window
            double scaleFactor = Math.Min(this.ActualWidth / 800, this.ActualHeight / 600);

            double LabelFontSize = 140 * scaleFactor;
            double ButtonFontSize = 40 * scaleFactor;
            double ButtonWidth = 300 * scaleFactor;

            saperLabelMenu.FontSize = LabelFontSize;
            btnEasy.FontSize = ButtonFontSize;
            btnNormal.FontSize = ButtonFontSize;
            btnHard.FontSize = ButtonFontSize;

            btnEasy.Width = ButtonWidth;
            btnNormal.Width = ButtonWidth;
            btnHard.Width = ButtonWidth;
        }

        private void SelectLvlButtonClick(object sender, RoutedEventArgs e)
        {
            grid_menu.Visibility = Visibility.Collapsed;

            Button btn = (Button)sender;

            if (btn.Name == "btnEasy"  ) { globalWidth = 8;  globalHeight = 8;  globalLevel = "easy";   }
            if (btn.Name == "btnNormal") { globalWidth = 16; globalHeight = 16; globalLevel = "normal"; }
            if (btn.Name == "btnHard"  ) { globalWidth = 32; globalHeight = 16; globalLevel = "hard";   }

            GenerateMap(globalWidth, globalHeight, globalLevel);
        }

        private void GenerateMap(int width, int height, string lvl)
        {
            width = globalWidth;
            height = globalHeight;

            globalNumOfBombs = (width * height) / 20;

            flagsLeft = globalNumOfBombs;

            // invicible extra wall
            width += 2;
            height += 2;

            // keeping window maximized
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;

            // setting button data depending on level
            int btnSize = 0;
            int btnFontSize = 0;
            int btnMargin = 0;

            if (lvl == "easy")
            {
                btnSize = 90;
                btnFontSize = 50;
                btnMargin = 2;
            }
            if (lvl == "normal")
            {
                btnSize = 50;
                btnFontSize = 30;
                btnMargin = 1;
            }
            if (lvl == "hard")
            {
                btnSize = 50;
                btnFontSize = 25;
                btnMargin = 1;
            }

            // making mapGrid dynamically
            mapGrid.VerticalAlignment = VerticalAlignment.Center;
            mapGrid.HorizontalAlignment = HorizontalAlignment.Center;

            // create row definitions
            for (int i = 0; i < height; i++)
            {
                mapGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(btnSize) });
            }

            // create column definitions
            for (int i = 0; i < width; i++)
            {
                mapGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(btnSize) });
            }

            // create buttons and add them to grid
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Button btn = new Button
                    {
                        FontSize = btnFontSize,
                        Margin = new Thickness(btnMargin),
                        Tag = "hide"
                    };

                    btn.Click  += LeftBtnClick;
                    btn.MouseRightButtonDown += RightBtnClick;

                    Grid.SetRow(btn, row);
                    Grid.SetColumn(btn, col);

                    mapGrid.Children.Add(btn);

                    PreventChangingColorOnHover(btn, hideBtnColor, hideBtnHoverColor);

                    // hide extra walls
                    if ((row == 0 || row == height - 1) || (col == 0 || col == width - 1))
                    {
                        btn.Visibility = Visibility.Collapsed;
                    }
                }
            }
            globalFieldsLeft = GetNumOfSafeFieldsLeft();

            AddStoperLabel();

            // placing headerGrid and mapGrid to mainGrid
            PlaceSubgridsToMainGrid();
        }

        bool isFirstClick = true;

        private void LeftBtnClick(object sender, RoutedEventArgs e)
        {
            Button clickedBtn = (Button)sender;

            if (isFirstClick)
            {
                isFirstClick = false;

                // sets bombs in random positions
                PlaceBombs(sender, e);

                // do the recursion thing
                ExploreEmptyArea(clickedBtn, Grid.GetRow(clickedBtn), Grid.GetColumn(clickedBtn));

                timer.Start();

                return;
            }

            if (clickedBtn.Tag.ToString() == "bomb" || clickedBtn.Tag.ToString() == "bomb_flag")
            {
                GridEndGame("You lost", YouLostLabelColor);
            }

            if (clickedBtn.Tag.ToString() == "hide" || clickedBtn.Tag.ToString() == "flag")
            {
                clickedBtn.Tag = "open";
                globalFieldsLeft = GetNumOfSafeFieldsLeft();
                clickedBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(openBtnColor));

                int bombsAround = CountNeighborBombs(Grid.GetRow(clickedBtn), Grid.GetColumn(clickedBtn));

                if (bombsAround > 0)
                {
                    clickedBtn.Content = bombsAround.ToString();
                }
            }

            if (globalFieldsLeft < 1)
            {
                timer.Stop();

                globalBestScore = GetBestScore();

                WriteScoreToFile();

                GridEndGame("You won", YouWonLabelColor);
            }
        }
        private void WriteScoreToFile()
        {
            string filePath = $"best-scores/{globalLevel}.txt";
            int[] arrScore = ConvertYourScoreStringToIntArray();

            int score = arrScore[0];

            if (arrScore.Length > 1)
            {
                score += (arrScore[1] * 60);
            }
            if (arrScore.Length > 2)
            {
                score += (arrScore[2] * 60 * 60);
            }

            globalYourScoreConverted = score;

            try
            {
                File.AppendAllText(filePath, globalYourScoreConverted.ToString() + '\n');
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private int[] ConvertYourScoreStringToIntArray()
        {
            string[] parts = timerContent.Split(':');

            int[] result = new int[parts.Length];

            for (int i = 0; i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out int value))
                {
                    result[i] = value;
                }
                else
                {
                    Console.WriteLine($"Error parsing '{parts[i]}' to an integer");
                }
            }
            Array.Reverse(result);

            return result;
        }

        private string GetBestScore()
        {
            string filePath = $"best-scores/{globalLevel}.txt";

            if (File.Exists(filePath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    
                    if (lines.Length > 0)
                    {
                        int[] allScores = lines.Select(int.Parse).ToArray();

                        int bestScore = allScores.Min();

                        // converting e.g. from 60 to 1:00 or from 100 to 1:40
                        string bestScoreFixed = "";

                        if (bestScore >= 60 * 60)
                        {
                            bestScore %= 3600;
                            bestScoreFixed += bestScore / 3600 + ':';
                        }
                        if (bestScore >= 60)
                        {
                            bestScore %= 60;
                            bestScoreFixed += bestScore / 60 + ':';
                        }
                        bestScoreFixed += bestScore.ToString();

                        return bestScoreFixed;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading scores from file: {ex.Message}");
                }
            }
            globalShowBestScore = false;

            return "-1";
        }

        private void RightBtnClick(object sender, RoutedEventArgs e)
        {
            // prevents placing flags before first click
            if (isFirstClick) { return; }

            Button btn = (Button)sender;

            if (btn.Tag.ToString() == "open") { return; }

            if (btn.Tag.ToString() == "flag")
            {
                btn.Tag = "hide";
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hideBtnColor));
                flagsLeft++;
                UpdateFlagLabel();
                return;
            }

            if (btn.Tag.ToString() == "bomb_flag")
            {
                btn.Tag = "bomb";
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hideBtnColor));
                flagsLeft++;
                UpdateFlagLabel();
                return;
            }

            if (flagsLeft <= 0) { return; }

            if (btn.Tag.ToString() == "bomb")
            {
                btn.Tag = "bomb_flag";
            }
            else
            {
                btn.Tag = "flag";
            }

            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(flagBtnColor));
            flagsLeft--;

            UpdateFlagLabel();
        }

        private void ExploreEmptyArea(Button btn, int row, int col)
        {
            // checks if in map zone
            if (row < 1 || col < 1 || row > mapGrid.RowDefinitions.Count - 1 || col > mapGrid.ColumnDefinitions.Count - 1) { return; }

            if (btn.Tag.ToString() == "open" || btn.Tag.ToString() == "bomb")
            {
                return;
            }

            btn.Tag = "open";
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(openBtnColor));

            int bombsAround = CountNeighborBombs(row, col);

            if (bombsAround == 0)
            {
                ExploreEmptyArea(GetButtonAtCoordinates(row - 1, col - 1), row - 1, col - 1);
                ExploreEmptyArea(GetButtonAtCoordinates(row - 1, col    ), row - 1, col    );
                ExploreEmptyArea(GetButtonAtCoordinates(row - 1, col + 1), row - 1, col + 1);
                ExploreEmptyArea(GetButtonAtCoordinates(row    , col - 1), row    , col - 1);
                ExploreEmptyArea(GetButtonAtCoordinates(row    , col + 1), row    , col + 1);
                ExploreEmptyArea(GetButtonAtCoordinates(row + 1, col - 1), row + 1, col - 1);
                ExploreEmptyArea(GetButtonAtCoordinates(row + 1, col    ), row + 1, col    );
                ExploreEmptyArea(GetButtonAtCoordinates(row + 1, col + 1), row + 1, col + 1);
            }
            else
            {
                btn.Content = bombsAround.ToString();
            }
        }

        private int CountNeighborBombs(int row, int col)
        {
            int bombCount = 0;

            for (int i = Math.Max(1, row - 1); i <= Math.Min(globalHeight, row + 1); i++)
            {
                for (int j = Math.Max(1, col - 1); j <= Math.Min(globalWidth, col + 1); j++)
                {
                    if (!(i == row && j == col) && (GetButtonAtCoordinates(i, j)?.Tag.ToString() == "bomb" || GetButtonAtCoordinates(i, j)?.Tag.ToString() == "bomb_flag"))
                    {
                        bombCount++;
                    }
                }
            }
            return bombCount;
        }

        private void PlaceBombs(object sender, RoutedEventArgs e)
        {
            Button firstClickBtn = (Button)sender;

            Random random = new Random();

            int firstClickedRow = Grid.GetRow(firstClickBtn);
            int firstClickedCol = Grid.GetColumn(firstClickBtn);

            for (int i = 0; i < globalNumOfBombs; i++)
            {
                int randomRow, randomCol;

                do 
                {
                    randomRow = random.Next(1, mapGrid.RowDefinitions.Count - 1);
                    randomCol = random.Next(1, mapGrid.ColumnDefinitions.Count - 1);
                } while (IsNeighboringButton(firstClickedRow, firstClickedCol, randomRow, randomCol));

                Button bombBtn = GetButtonAtCoordinates(randomRow, randomCol);

                if (bombBtn != null && bombBtn.Tag.ToString() != "bomb")
                {
                    bombBtn.Tag = "bomb";
                }
                else
                {
                    i--;
                }
            }
        }

        private Button GetButtonAtCoordinates(int row, int col)
        {
            foreach (UIElement element in mapGrid.Children)
            {
                if (element is Button button && Grid.GetRow(button) == row && Grid.GetColumn(button) == col)
                {
                    return button;
                }
            }
            return null;
        }

        private bool IsNeighboringButton(int clickedRow, int clickedCol, int testRow, int testCol)
        {
            return Math.Abs(clickedRow - testRow) <= 1 && Math.Abs(clickedCol - testCol) <= 1;
        }

        private DispatcherTimer timer;
        private TimeSpan elapsedTime;
        private void AddStoperLabel()
        {

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            elapsedTime = elapsedTime.Add(TimeSpan.FromSeconds(1));
            UpdateTimeLabel();
        }

        string timerContent = "0";

        private void UpdateTimeLabel()
        {
            if (elapsedTime.TotalHours >= 1)
            {
                timerContent = $"{(int)elapsedTime.TotalHours:D}:{elapsedTime.Minutes:D2}:{elapsedTime.Seconds:D2}";
            }
            else if (elapsedTime.TotalMinutes >= 1)
            {
                timerContent = $"{elapsedTime.Minutes:D}:{elapsedTime.Seconds:D2}";
            }
            else
            {
                timerContent = $"{elapsedTime.Seconds:D}";
            }

            timerLabel.Content = "Timer: " + timerContent;
        }

        int flagsLeft = 0;

        Label flag = new Label
        {
            Content = "Flags: ?",
            FontSize = 50,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(flagsLabelColor)),
            Margin = new Thickness(300, 0, 0, 0)
        };

        Label timerLabel = new Label
        {
            Content = "Timer: 0",
            FontSize = 50,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(timerLabelColor)),
            Margin = new Thickness(75, 0, 0, 0)
        };

        private void AddHeaderLabel()
        {
            headerGrid.HorizontalAlignment = HorizontalAlignment.Center;
            headerGrid.VerticalAlignment = VerticalAlignment.Center;

            headerGrid.Margin = new Thickness(0, 0, 0, 0);

            headerGrid.RowDefinitions.Add(new RowDefinition());
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(600) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(600) });

            // adding flag to headerGrid
            UpdateFlagLabel();

            flag.HorizontalAlignment = HorizontalAlignment.Left;

            Grid.SetRow(flag, 0);
            Grid.SetColumn(flag, 0);

            headerGrid.Children.Add(flag);

            // adding stoper to headerGrid
            timerLabel.HorizontalAlignment = HorizontalAlignment.Left;

            Grid.SetRow(timerLabel, 0);
            Grid.SetColumn(timerLabel, 1);

            headerGrid.Children.Add(timerLabel);

            // adding headerGrid to mainGrid
            Grid.SetRow(headerGrid, 0);
            Grid.SetColumn(headerGrid, 0);

            mainGrid.Children.Add(headerGrid);
        }

        private void PlaceSubgridsToMainGrid()
        {
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(100) });
            mainGrid.RowDefinitions.Add(new RowDefinition());
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition());

            AddHeaderLabel();

            Grid.SetRow(mapGrid, 1);

            mainGrid.Children.Add(mapGrid);

            Content = mainGrid;
        }

        private void UpdateFlagLabel()
        {
            flag.Content = "Flags: " + flagsLeft;
        }

        // By default, when you hover over a button, its color changes to light blue. This function prevents this behavior
        private void PreventChangingColorOnHover(Button btn, string color, string colorHover)
        {
            Style buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(color))));

            ControlTemplate template = new ControlTemplate(typeof(Button));
            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetBinding(Border.BackgroundProperty, new Binding("Background") { RelativeSource = RelativeSource.TemplatedParent });

            FrameworkElementFactory contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));

            Trigger mouseOverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHover))));

            buttonStyle.Triggers.Add(mouseOverTrigger);

            btn.Style = buttonStyle;
        }

        private void PreventChangingColorOnHoverInMenuButtons(string color, string colorHover)
        {
            Button btnEasy = (Button)FindName("btnEasy");
            Button btnNormal = (Button)FindName("btnNormal");
            Button btnHard = (Button)FindName("btnHard");

            PreventChangingColorOnHover(btnEasy, color, colorHover);
            PreventChangingColorOnHover(btnNormal, color, colorHover);
            PreventChangingColorOnHover(btnHard, color, colorHover);
        }

        private void SetMenuButtonsColor()
        {
            Button btnEasy = (Button)FindName("btnEasy");
            Button btnNormal = (Button)FindName("btnNormal");
            Button btnHard = (Button)FindName("btnHard");

            btnEasy.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(menuBtnsFontColor));
            btnNormal.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(menuBtnsFontColor));
            btnHard.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(menuBtnsFontColor));
        }

        private void GridEndGame(string endText, string textColor)
        {
            Label endGameLabel = new Label
            {
                Content = endText,
                FontSize = 150,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(textColor)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Label yourScore = new Label
            {
                Content = "Your score: " + timerContent,
                FontSize = 50,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(yourScoreColor)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Label bestScore = new Label
            {
                Content = "Best score: " + globalBestScore,
                FontSize = 50,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bestScoreColor)),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Label newRecord = new Label
            {
                Content = "Congratulations, you've scored a new record",
                FontSize = 50,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(newRecordColor)),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 150, 0, 0)
            };

            Grid endGame = new Grid();

            endGame.RowDefinitions.Add(new RowDefinition());
            endGame.RowDefinitions.Add(new RowDefinition());
            endGame.RowDefinitions.Add(new RowDefinition());
            endGame.RowDefinitions.Add(new RowDefinition());
            endGame.ColumnDefinitions.Add(new ColumnDefinition());

            endGame.VerticalAlignment = VerticalAlignment.Center;
            endGame.HorizontalAlignment = HorizontalAlignment.Center;

            // adding endGameLabel to endGame grid
            Grid.SetRow(endGameLabel, 0);
            Grid.SetColumn(endGameLabel, 0);

            endGame.Children.Add(endGameLabel);

            if (endText == "You won")
            {
                // adding yourScore to endGame grid
                Grid.SetRow(yourScore, 1);
                Grid.SetColumn(yourScore, 0);

                endGame.Children.Add(yourScore);

                if (globalShowBestScore)
                {
                    // adding bestScore to endGame grid
                    Grid.SetRow(bestScore, 2);
                    Grid.SetColumn(bestScore, 0);

                    endGame.Children.Add(bestScore);
                }

                if (globalYourScoreConverted < int.Parse(globalBestScore))
                {
                    // adding newRecord to endGame grid
                    Grid.SetRow(newRecord, 4);
                    Grid.SetColumn(newRecord, 0);

                    endGame.Children.Add(newRecord);
                }
            }

            Content = endGame;
        }

        private int GetNumOfSafeFieldsLeft()
        {
            int res = 0;

            foreach (UIElement element in mapGrid.Children)
            {
                if (element is Button btn && btn.Tag.ToString() == "hide" && btn.Visibility != Visibility.Collapsed)
                {
                    res++;
                }
            }
            return res;
        }

        private void DisplayMenu()
        {
            grid_menu.Visibility = Visibility.Visible;
        }

        private void SetWindowBgColor(string color)
        {
            mainWindow.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        }

        private void WindowAlignCenter()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}