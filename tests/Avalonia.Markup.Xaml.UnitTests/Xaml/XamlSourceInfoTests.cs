using System;
using System.Linq;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Markup.Xaml.Diagnostics;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class XamlSourceInfoTests : XamlTestBase
    {
        private static readonly RuntimeXamlLoaderConfiguration s_configuration = new RuntimeXamlLoaderConfiguration
        {
            CreateSourceInfo = true
        };

        [Theory]
        [InlineData(@"C:\TestFolder\TestFile.xaml")] // Windows-style path
        [InlineData("/TestFolder/TestFile.xaml")] // Unix-style path
        public void Root_UserControl_With_BaseUri_Gets_XamlSourceInfo_SourceUri_Set(string document)
        {
            var xamlDocument = new RuntimeXamlLoaderDocument(
                """
                <UserControl xmlns='https://github.com/avaloniaui'
                     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
                </UserControl>
                """)
            {
                Document = document
            };

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xamlDocument, s_configuration);

            var sourceInfo = XamlSourceInfo.GetXamlSourceInfo(userControl);

            Assert.NotNull(sourceInfo);
            Assert.Equal("file", sourceInfo.SourceUri!.Scheme);
            Assert.True(sourceInfo.SourceUri!.IsAbsoluteUri);
            Assert.Equal(new UriBuilder("file", "") {Path = document}.Uri, sourceInfo.SourceUri);
        }

        [Fact]
        public void Root_UserControl_Gets_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);

            var sourceInfo = XamlSourceInfo.GetXamlSourceInfo(userControl);

            Assert.NotNull(sourceInfo);
        }

        [Fact]
        public void Nested_Controls_All_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <StackPanel>
        <Button />
        <TextBlock />
    </StackPanel>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var stackPanel = (StackPanel)userControl.Content!;
            var button = (Button)stackPanel.Children[0];
            var textblock = (TextBlock)stackPanel.Children[1];

            var userControlSourceInfo = XamlSourceInfo.GetXamlSourceInfo(userControl);
            Assert.NotNull(userControlSourceInfo);

            var stackPanelSourceInfo = XamlSourceInfo.GetXamlSourceInfo(stackPanel);
            Assert.NotNull(stackPanelSourceInfo);

            var buttonSourceInfo = XamlSourceInfo.GetXamlSourceInfo(button);
            Assert.NotNull(buttonSourceInfo);

            var textblockSourceInfo = XamlSourceInfo.GetXamlSourceInfo(textblock);
            Assert.NotNull(textblockSourceInfo);
        }

        [Fact]
        public void Property_Elements_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Rectangle Fill=""Blue"" Width=""63"" Height=""41"">
        <Rectangle.OpacityMask>
            <LinearGradientBrush StartPoint=""0%,0%"" EndPoint=""100%,100%"">
                <LinearGradientBrush.GradientStops>
                    <GradientStop Offset=""0"" Color=""Black""/>
                    <GradientStop Offset=""1"" Color=""Transparent""/>
                </LinearGradientBrush.GradientStops>
            </LinearGradientBrush>
        </Rectangle.OpacityMask>
    </Rectangle>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var rect = (Rectangle)userControl.Content!;
            var gradient = (LinearGradientBrush)rect.OpacityMask!;
            var stopOne = (GradientStop)gradient.GradientStops.First();
            var stopTwo = (GradientStop)gradient.GradientStops.Last();

            var rectSourceInfo = XamlSourceInfo.GetXamlSourceInfo(rect);
            Assert.NotNull(rectSourceInfo);

            var gradientSourceInfo = XamlSourceInfo.GetXamlSourceInfo(gradient);
            Assert.NotNull(gradientSourceInfo);

            var stopOneSourceInfo = XamlSourceInfo.GetXamlSourceInfo(stopOne);
            Assert.NotNull(stopOneSourceInfo);

            var stopTwoSourceInfo = XamlSourceInfo.GetXamlSourceInfo(stopTwo);
            Assert.NotNull(stopTwoSourceInfo);
        }

        [Fact]
        public void Shapes_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <Canvas Name=""TheCanvas"" Background=""Yellow"" Width=""300"" Height=""400"">
        <Ellipse Fill=""Green"" Width=""58"" Height=""58"" Canvas.Left=""88"" Canvas.Top=""100""/>
        <Path Fill=""Orange"" Canvas.Left=""30"" Canvas.Top=""250""/>
        <Path Fill=""OrangeRed"" Canvas.Left=""180"" Canvas.Top=""250"">
            <Path.Data>
                <PathGeometry>
                    <PathFigure StartPoint=""0,0"" IsClosed=""True"">
                        <QuadraticBezierSegment Point1=""50,0"" Point2=""50,-50"" />
                        <QuadraticBezierSegment Point1=""100,-50"" Point2=""100,0"" />
                        <LineSegment Point=""50,0"" />
                        <LineSegment Point=""50,50"" />
                    </PathFigure>
                </PathGeometry>
            </Path.Data>
        </Path>
        <Line StartPoint=""120,185"" EndPoint=""30,115"" Stroke=""Red"" StrokeThickness=""2""/>
        <Polygon Points=""75,0 120,120 0,45 150,45 30,120"" Stroke=""DarkBlue"" StrokeThickness=""1"" Fill=""Violet"" Canvas.Left=""150"" Canvas.Top=""31""/>
    </Canvas>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var canvas = (Canvas)userControl.Content!;
            var ellipse = (Ellipse)canvas.Children[0];
            var path1 = (Path)canvas.Children[1];
            var path2 = (Path)canvas.Children[2];
            var geometry = (PathGeometry)path2.Data!;
            var figure = (PathFigure)geometry.Figures![0];
            var segment1 = figure.Segments![0];
            var segment2 = figure.Segments![1];
            var segment3 = figure.Segments![2];
            var segment4 = figure.Segments![3];
            var line = (Line)canvas.Children[3];
            var polygon = (Polygon)canvas.Children[4];

            var canvasSourceInfo = XamlSourceInfo.GetXamlSourceInfo(canvas);
            Assert.NotNull(canvasSourceInfo);

            var ellipseSourceInfo = XamlSourceInfo.GetXamlSourceInfo(ellipse);
            Assert.NotNull(ellipseSourceInfo);

            var path1SourceInfo = XamlSourceInfo.GetXamlSourceInfo(path1);
            Assert.NotNull(path1SourceInfo);

            var path2SourceInfo = XamlSourceInfo.GetXamlSourceInfo(path2);
            Assert.NotNull(path2SourceInfo);

            var geometrySourceInfo = XamlSourceInfo.GetXamlSourceInfo(geometry);
            Assert.NotNull(geometrySourceInfo);

            var figureSourceInfo = XamlSourceInfo.GetXamlSourceInfo(figure);
            Assert.NotNull(figureSourceInfo);

            var segment1SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment1);
            Assert.NotNull(segment1SourceInfo);

            var segment2SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment2);
            Assert.NotNull(segment2SourceInfo);

            var segment3SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment3);
            Assert.NotNull(segment3SourceInfo);

            var segment4SourceInfo = XamlSourceInfo.GetXamlSourceInfo(segment4);
            Assert.NotNull(segment4SourceInfo);

            var lineSourceInfo = XamlSourceInfo.GetXamlSourceInfo(line);
            Assert.NotNull(lineSourceInfo);

            var polygonSourceInfo = XamlSourceInfo.GetXamlSourceInfo(polygon);
            Assert.NotNull(polygonSourceInfo);
        }

        [Fact]
        public void Styles_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Styles>
		<Style Selector=""Button"">
			<Setter Property=""Margin"" Value=""5"" />
		</Style>
		<ContainerQuery Name=""container""
						Query=""max-width:400"">
			<Style Selector=""Button"">
				<Setter Property=""Background""
						Value=""Red""/>
			</Style>
		</ContainerQuery>
    </UserControl.Styles>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var style = (Style)userControl.Styles[0];
            var query = (ContainerQuery)userControl.Styles[1];

            var styleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(style);
            Assert.NotNull(styleSourceInfo);

            var querySourceInfo = XamlSourceInfo.GetXamlSourceInfo(query);
            Assert.NotNull(querySourceInfo);
        }

        [Fact]
        public void Animations_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Styles>
		<Style Selector=""Rectangle.red"">
			<Setter Property=""Fill"" Value=""Red""/>
			<Style.Animations>
				<Animation Duration=""0:0:3"">
					<KeyFrame Cue=""0%"">
						<Setter Property=""Opacity"" Value=""0.0""/>
					</KeyFrame>
					<KeyFrame Cue=""100%"">
						<Setter Property=""Opacity"" Value=""1.0""/>
					</KeyFrame>
				</Animation>
			</Style.Animations>
		</Style>
    </UserControl.Styles>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var style = (Style)userControl.Styles[0];
            var animation = (Animation.Animation)style.Animations[0];
            var frame1 = animation.Children[0];
            var frame2 = animation.Children[1];

            var styleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(style);
            Assert.NotNull(styleSourceInfo);

            var animationSourceInfo = XamlSourceInfo.GetXamlSourceInfo(animation);
            Assert.NotNull(animationSourceInfo);

            var frameOneSourceInfo = XamlSourceInfo.GetXamlSourceInfo(frame1);
            Assert.NotNull(frameOneSourceInfo);

            var frameTwoSourceInfo = XamlSourceInfo.GetXamlSourceInfo(frame2);
            Assert.NotNull(frameTwoSourceInfo);
        }

        [Fact]
        public void DataTemplates_And_Deferred_Contents_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
     xmlns:local='using:Avalonia.Markup.Xaml.UnitTests.Xaml'>
	<UserControl.DataTemplates>
		<DataTemplate DataType=""local:SourceInfoTestViewModel"">
			<Border Background=""Red"" CornerRadius=""8"">
				<TextBox Text=""{Binding Name}""/>
			</Border>
		</DataTemplate>
	</UserControl.DataTemplates>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var datatemplate = (DataTemplate)userControl.DataTemplates[0];

            Border border;

            // The template and it's content is deferred as not used (yet)
            if (datatemplate.Content is IDeferredContent deferredContent)
            {
                var templateResult = (ITemplateResult)deferredContent.Build(null)!;
                border = (Border)templateResult.Result!;
            }
            else
            {
                border = (Border)datatemplate.Content!;
            }

            var textBox = (TextBox)border!.Child!;

            var datatemplateSourceInfo = XamlSourceInfo.GetXamlSourceInfo(datatemplate);
            Assert.NotNull(datatemplateSourceInfo);

            var borderSourceInfo = XamlSourceInfo.GetXamlSourceInfo(border);
            Assert.NotNull(borderSourceInfo);

            var textBoxSourceInfo = XamlSourceInfo.GetXamlSourceInfo(textBox);
            Assert.NotNull(textBoxSourceInfo);
        }

        [Fact]
        public void Resources_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
		<ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
		        <ResourceDictionary x:Key='Light'>
		            <SolidColorBrush x:Key='BackgroundBrush' Color='White'/>
		            <SolidColorBrush x:Key='ForegroundBrush' Color='Black'/>
		        </ResourceDictionary>
		        <ResourceDictionary x:Key='Dark'>
		            <SolidColorBrush x:Key='BackgroundBrush' Color='Black'/>
		            <SolidColorBrush x:Key='ForegroundBrush' Color='White'/>
		        </ResourceDictionary>
		    </ResourceDictionary.ThemeDictionaries>

		    <SolidColorBrush x:Key=""Background"" Color=""Yellow"" />
		    <SolidColorBrush x:Key='OtherBrush'>Black</SolidColorBrush>

		</ResourceDictionary>
	</UserControl.Resources>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var backgroundBrush = userControl.Resources["Background"];
            var lightDictionary = (ResourceDictionary)userControl.Resources.ThemeDictionaries[ThemeVariant.Light];
            var darkDictionary = (ResourceDictionary)userControl.Resources.ThemeDictionaries[ThemeVariant.Dark];
            var lightForeground = lightDictionary["ForegroundBrush"];
            var darkBackground = lightDictionary["BackgroundBrush"];
            var otherBrush = userControl.Resources["OtherBrush"];

            var backgroundBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(backgroundBrush!);
            Assert.NotNull(backgroundBrushSourceInfo);

            var lightDictionarySourceInfo = XamlSourceInfo.GetXamlSourceInfo(lightDictionary!);
            Assert.NotNull(lightDictionarySourceInfo);

            var darkDictionarySourceInfo = XamlSourceInfo.GetXamlSourceInfo(darkDictionary!);
            Assert.NotNull(darkDictionarySourceInfo);

            var lightForegroundSourceInfo = XamlSourceInfo.GetXamlSourceInfo(lightForeground!);
            Assert.NotNull(lightForegroundSourceInfo);

            var darkBackgroundSourceInfo = XamlSourceInfo.GetXamlSourceInfo(darkBackground!);
            Assert.NotNull(darkBackgroundSourceInfo);

            var otherBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(otherBrush!);
            Assert.NotNull(otherBrushSourceInfo);
        }

        [Fact]
        public void ResourceDictionary_Value_Types_Do_Not_Set_XamlSourceInfo()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
		<x:String x:Key='text'>foobar</x:String>
		<x:Double x:Key=""A_Double"">123.3</x:Double>
		<x:Int16 x:Key=""An_Int16"">123</x:Int16>
		<x:Int32 x:Key=""An_Int32"">37434323</x:Int32>
		<Thickness x:Key=""PreferredPadding"">10,20,10,0</Thickness>
	</UserControl.Resources>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var foobarString = userControl.Resources["text"];
            var aDouble = userControl.Resources["A_Double"];
            var anInt16 = userControl.Resources["An_Int16"];
            var anInt32 = userControl.Resources["An_Int32"];
            var padding = userControl.Resources["PreferredPadding"];

            // Value types shouldn't get source info
            var foobarStringSourceInfo = XamlSourceInfo.GetXamlSourceInfo(foobarString!);
            Assert.Null(foobarStringSourceInfo);

            var aDoubleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(aDouble!);
            Assert.Null(aDoubleSourceInfo);

            var anInt16SourceInfo = XamlSourceInfo.GetXamlSourceInfo(anInt16!);
            Assert.Null(anInt16SourceInfo);

            var anInt32SourceInfo = XamlSourceInfo.GetXamlSourceInfo(anInt32!);
            Assert.Null(anInt32SourceInfo);

            var paddingSourceInfo = XamlSourceInfo.GetXamlSourceInfo(padding!);
            Assert.Null(paddingSourceInfo);
        }

        [Fact]
        public void ResourceDictionary_Set_Resource_Source_Info()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
		<x:String x:Key='text'>foobar</x:String>
		<x:Double x:Key=""A_Double"">123.3</x:Double>
		<x:Int16 x:Key=""An_Int16"">123</x:Int16>
		<x:Int32 x:Key=""An_Int32"">37434323</x:Int32>
		<Thickness x:Key=""PreferredPadding"">10,20,10,0</Thickness>
        <x:Uri x:Key='homepage'>http://avaloniaui.net</x:Uri>
        <SolidColorBrush x:Key='MyBrush' Color='Red'/>
	</UserControl.Resources>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var resources = userControl.Resources;

            var foobarStringSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "text");
            Assert.NotNull(foobarStringSourceInfo);

            var aDoubleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "A_Double");
            Assert.NotNull(aDoubleSourceInfo);

            var anInt16SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int16");
            Assert.NotNull(anInt16SourceInfo);

            var anInt32SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int32");
            Assert.NotNull(anInt32SourceInfo);

            var paddingSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "PreferredPadding");
            Assert.NotNull(paddingSourceInfo);

            var homepageSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "homepage");
            Assert.NotNull(homepageSourceInfo);

            var myBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "MyBrush");
            Assert.NotNull(myBrushSourceInfo);
        }

        [Fact]
        public void ResourceDictionary_Set_Resource_Source_Info_With_Nested_Dictionaries()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Resources>
        <ResourceDictionary>
            <x:String x:Key='text'>foobar</x:String>
            <x:Double x:Key=""A_Double"">123.3</x:Double>
            <x:Int16 x:Key=""An_Int16"">123</x:Int16>
            <x:Int32 x:Key=""An_Int32"">37434323</x:Int32>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary>
                    <Thickness x:Key=""PreferredPadding"">10,20,10,0</Thickness>
                    <x:Uri x:Key='homepage'>http://avaloniaui.net</x:Uri>
                </ResourceDictionary>
            </ResourceDictionary.MergedDictionaries>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key='Light'>
                    <SolidColorBrush x:Key='MyBrush' Color='Red'/>
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
	</UserControl.Resources>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var resources = userControl.Resources;
            var innerResources = (IResourceDictionary)resources.MergedDictionaries[0];
            var themeResources = (IResourceDictionary)resources.ThemeDictionaries[ThemeVariant.Light];

            // Outer define source info
            var foobarStringSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "text");
            Assert.NotNull(foobarStringSourceInfo);

            var aDoubleSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "A_Double");
            Assert.NotNull(aDoubleSourceInfo);

            var anInt16SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int16");
            Assert.NotNull(anInt16SourceInfo);

            var anInt32SourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "An_Int32");
            Assert.NotNull(anInt32SourceInfo);

            // Outer one should not have source info for inner resources
            var paddingSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "PreferredPadding");
            Assert.Null(paddingSourceInfo);

            var homepageSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "homepage");
            Assert.Null(homepageSourceInfo);

            var myBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(resources, "MyBrush");
            Assert.Null(myBrushSourceInfo);

            // Inner defined source info
            homepageSourceInfo = XamlSourceInfo.GetXamlSourceInfo(innerResources, "homepage");
            Assert.NotNull(homepageSourceInfo);

            myBrushSourceInfo = XamlSourceInfo.GetXamlSourceInfo(themeResources, "MyBrush");
            Assert.NotNull(myBrushSourceInfo);

            // Non-value types should have source info themselves
            var homepage = XamlSourceInfo.GetXamlSourceInfo(innerResources["homepage"]!);
            Assert.NotNull(homepage);

            var myBrush = XamlSourceInfo.GetXamlSourceInfo(themeResources["MyBrush"]!);
            Assert.NotNull(myBrush);
        }

        [Fact]
        public void Gestures_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.GestureRecognizers>
		<ScrollGestureRecognizer CanHorizontallyScroll=""True""
								 CanVerticallyScroll=""True""/>
		<PullGestureRecognizer PullDirection=""TopToBottom""/>
	</UserControl.GestureRecognizers>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var scroll = (ScrollGestureRecognizer)userControl.GestureRecognizers.First();
            var pull = (PullGestureRecognizer)userControl.GestureRecognizers.Last();

            var scrollSourceInfo = XamlSourceInfo.GetXamlSourceInfo(scroll);
            Assert.NotNull(scrollSourceInfo);

            var pullSourceInfo = XamlSourceInfo.GetXamlSourceInfo(pull);
            Assert.NotNull(pullSourceInfo);
        }

        [Fact]
        public void Transitions_Get_XamlSourceInfo_Set()
        {
            var xaml = new RuntimeXamlLoaderDocument(@"
<UserControl xmlns='https://github.com/avaloniaui'
     xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
	<UserControl.Transitions>
		<Transitions>
			<DoubleTransition Property=""Width"" Duration=""0:0:1.5""/>
			<DoubleTransition Property=""Height"" Duration=""0:0:1.5""/>
		</Transitions>
	</UserControl.Transitions>
</UserControl>");

            var userControl = (UserControl)AvaloniaRuntimeXamlLoader.Load(xaml, s_configuration);
            var width = (DoubleTransition)userControl.Transitions!.First();
            var height = (DoubleTransition)userControl.Transitions!.Last();

            var widthSourceInfo = XamlSourceInfo.GetXamlSourceInfo(width);
            Assert.NotNull(widthSourceInfo);

            var heightSourceInfo = XamlSourceInfo.GetXamlSourceInfo(height);
            Assert.NotNull(heightSourceInfo);
        }
    }

    public class SourceInfoTestViewModel
    {
        public string? Name { get; set; }
    }
}
