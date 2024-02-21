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

namespace saper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    // Tag legend:
    //
    // "hide"
    // "open"
    // "bomb"
    // "flag"
    // "bomb_flag"

    public partial class MainWindow : Window
    {
        int globalWidth = 0;
        int globalHeight = 0;
        public MainWindow()
        {
            InitializeComponent();

            grid_menu.Visibility = Visibility.Visible;
        }

        private void SelectLvlButtonClick(object sender, RoutedEventArgs e)
        {
            grid_menu.Visibility = Visibility.Collapsed;

            Button btn = (Button)sender;

            if (btn.Name == "btnEasy")   { GenerateMap( 8,  8, "easy"  ); }
            if (btn.Name == "btnNormal") { GenerateMap(16, 16, "normal"); }
            if (btn.Name == "btnHard")   { GenerateMap(32, 16, "hard"  ); }
        }

        Grid mainGrid = new Grid();
        Grid flagGrid = new Grid();
        Grid mapGrid = new Grid();


        private void GenerateMap(int width, int height, string lvl)
        {
            globalWidth = width;
            globalHeight = height;

            // invicible extra wall
            width += 2;
            height += 2;

            // keeping window maximized
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.NoResize;

            // setting needed data depending on level
            int btnSize = 0;
            int btnFontSize = 0;
            int btnMargin = 0;


            if (lvl == "easy")
            {
                btnSize = 90;
                btnFontSize = 50;
                btnMargin = 2;
                flagsLeft = 21;
            }
            if (lvl == "normal")
            {
                btnSize = 50;
                btnFontSize = 30;
                btnMargin = 1;
                flagsLeft = 85;
            }
            if (lvl == "hard")
            {
                btnSize = 50;
                btnFontSize = 25;
                btnMargin = 1;
                flagsLeft = 171;
            }

            // making grid
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
                        /*Content = "hi",*/
                        FontSize = btnFontSize,
                        Margin = new Thickness(btnMargin),
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        VerticalAlignment = VerticalAlignment.Stretch,
                        Tag = "hide"
                    };

                    btn.Click  += LeftBtnClick;
                    btn.MouseRightButtonDown += RightBtnClick;

                    // Set the row and column properties
                    Grid.SetRow(btn, row);
                    Grid.SetColumn(btn, col);

                    // Add the button to the main grid
                    mapGrid.Children.Add(btn);

                    DontChangeColorOnHover(btn);

                    if ((row == 0 || row == height - 1) || (col == 0 || col == width - 1))
                    {
                        /*btn.Opacity = 0;*/
                        btn.Visibility = Visibility.Collapsed;
                    }
                }
            }

            // placing 2 sub grids to main grid
            mainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(100) });
            mainGrid.RowDefinitions.Add(new RowDefinition());
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition());

            AddFlagLabel();

            Grid.SetRow(mapGrid, 1);

            mainGrid.Children.Add(mapGrid);

            Content = mainGrid;
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
                exploreEmptyArea(clickedBtn, Grid.GetRow(clickedBtn), Grid.GetColumn(clickedBtn));

                return;
            }

            if (clickedBtn.Tag.ToString() == "bomb" || clickedBtn.Tag.ToString() == "bomb_flag")
            {
                Grid endGame = new Grid();

                endGame.RowDefinitions.Add(new RowDefinition());
                endGame.ColumnDefinitions.Add(new ColumnDefinition());

                endGame.VerticalAlignment = VerticalAlignment.Center;
                endGame.HorizontalAlignment = HorizontalAlignment.Center;

                Label youLostText = new Label
                {
                    Content = "You lost",
                    FontSize = 150,
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e01a4f"))
                };

                Grid.SetRow(youLostText, 0);
                Grid.SetColumn(youLostText, 0);

                endGame.Children.Add(youLostText);

                Content = endGame;
            }

            if (clickedBtn.Tag.ToString() == "hide")
            {
                clickedBtn.Tag = "open";
                clickedBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f4f3ee"));

                int bombsAround = CountNeighborBombs(Grid.GetRow(clickedBtn), Grid.GetColumn(clickedBtn));

                if (bombsAround > 0)
                {
                    clickedBtn.Content = bombsAround.ToString();
                }
            }
        }

        private void RightBtnClick(object sender, RoutedEventArgs e)
        {
            if (isFirstClick) { return; }

            Button btn = (Button)sender;

            if (btn.Tag.ToString() == "open") { return; }

            if (btn.Tag.ToString() == "flag")
            {
                btn.Tag = "hide";
                btn.Background = Brushes.LightYellow;
                flagsLeft++;
                UpdateFlagLabel();
                return;
            }

            if (btn.Tag.ToString() == "bomb_flag")
            {
                btn.Tag = "bomb";
                btn.Background = Brushes.LightYellow;
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

            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9c22e"));
            flagsLeft--;

            UpdateFlagLabel();
        }

        private void exploreEmptyArea(Button btn, int row, int col)
        {
            // checks if in map zone
            if (row < 1 || col < 1 || row > mapGrid.RowDefinitions.Count - 1 || col > mapGrid.ColumnDefinitions.Count - 1) { return; }

            if (btn.Tag.ToString() == "number")
            {
                btn.Content = "num";
                return;
            }

            if (btn.Tag.ToString() == "open" || btn.Tag.ToString() == "bomb")
            {
                return;
            }

            btn.Tag = "open";
            /*btn.Content = "-";*/
            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f4f3ee"));

            int bombsAround = CountNeighborBombs(row, col);

            if (bombsAround == 0)
            {
                exploreEmptyArea(GetButtonAtCoordinates(row - 1, col - 1), row - 1, col - 1);
                exploreEmptyArea(GetButtonAtCoordinates(row - 1, col    ), row - 1, col    );
                exploreEmptyArea(GetButtonAtCoordinates(row - 1, col + 1), row - 1, col + 1);
                exploreEmptyArea(GetButtonAtCoordinates(row    , col - 1), row    , col - 1);
                exploreEmptyArea(GetButtonAtCoordinates(row    , col + 1), row    , col + 1);
                exploreEmptyArea(GetButtonAtCoordinates(row + 1, col - 1), row + 1, col - 1);
                exploreEmptyArea(GetButtonAtCoordinates(row + 1, col    ), row + 1, col    );
                exploreEmptyArea(GetButtonAtCoordinates(row + 1, col + 1), row + 1, col + 1);
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

            int numOfBombs = (globalWidth * globalHeight) / 5;

            int firstClickedRow = Grid.GetRow(firstClickBtn);
            int firstClickedCol = Grid.GetColumn(firstClickBtn);

            for (int i = 0; i < numOfBombs; i++)
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
                    /*bombBtn.Content = "💣";*/
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
            // Check if the tested button is neighboring the clicked button
            return Math.Abs(clickedRow - testRow) <= 1 && Math.Abs(clickedCol - testCol) <= 1;
        }


        int flagsLeft = 0;

        Label flag = new Label
        {
            Content = "Flags: ",
            FontSize = 50,
            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#e01a4f"))
        };

        private void AddFlagLabel()
        {
            UpdateFlagLabel();

            /*flagGrid.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fff"));*/
            flagGrid.Margin = new Thickness(0, 0, 0, 0);

            flagGrid.RowDefinitions.Add(new RowDefinition());
            flagGrid.ColumnDefinitions.Add(new ColumnDefinition());

            flagGrid.HorizontalAlignment = HorizontalAlignment.Center;
            flagGrid.VerticalAlignment = VerticalAlignment.Center;

            Grid.SetRow(flag, 0);
            Grid.SetColumn(flag, 0);

            flagGrid.Children.Add(flag);

            Grid.SetRow(flagGrid, 0);
            Grid.SetColumn(flagGrid, 0);

            mainGrid.Children.Add(flagGrid);
        }

        private void UpdateFlagLabel()
        {
            flag.Content = "Flags: " + flagsLeft;
        }

        private void DontChangeColorOnHover(Button btn)
        {
            Style buttonStyle = new Style(typeof(Button));
            buttonStyle.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.LightYellow));

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
            mouseOverTrigger.Setters.Add(new Setter(Button.BackgroundProperty, Brushes.LightYellow));

            buttonStyle.Triggers.Add(mouseOverTrigger);

            btn.Style = buttonStyle;
        }
    }
}