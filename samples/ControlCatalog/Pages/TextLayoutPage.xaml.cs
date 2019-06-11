using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class TextLayoutPage : UserControl
    {
        public TextLayoutPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);


            var sheep = this.FindControl<TextBlock>("Sheep");
            sheep.Text = "";
            sheep.FontFamily = new Avalonia.Media.FontFamily("Liberation Sans");
            sheep.Width=200;
            sheep.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
            var rnd = new Random();
            this.FindControl<Button>("Wolf").Click += (a, b) =>
                        {
                            var pos = rnd.Next(sheep.Text.Length);
                            sheep.Text = sheep.Text.Insert(pos,
                 System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(
                 @"
 LgouCnTMkM2DzZLNgcyOzI7MkMyVzZDNksywzKzNmcyhzJnMp82OzLLMlsy2Zc2EzZ7Mhc2QzILMvcyGzYHNiMyczZbNlM2WzYXMs82NzZPMn3PMlcyAzIvMhM2hzI/NnsyQzIbMgcyZzK/NlM2TzJ3MrM2izJbMscywzLd0zZHMjM2hzYPMkc2KzZ3Mh8yIzL/NlcyzzKbMrs2ZzKXMns2izZTMtGnMlM2XzJDMjMyTzKTNicyezJ/Mo8y5zLVuzI/MksyazYbMksyQzI3NhsyFzJnNic2fzLrMvMyszKLMn82aZ82EzIrMis2YzYPMjcyGzIvMu82TzZnMqsyjzZPMtAouCi7wn5GJ8J+YjvCfkYnml6XmnKzoqp7jga7jgq3jg7zjg5zjg7zjg4lhYmNkZfCfkajigI0g8J+RqOKAjfCfkanigI0g8J+RqOKAjfCfkanigI3wn5Gn4oCNIPCfkajigI3wn5Gp4oCN8J+Rp+KAjfCfkacK4Zqg4ZuH4Zq74Zur4ZuS4Zum4Zqm4Zur4Zqg4Zqx4Zqp4Zqg4Zqi4Zqx4Zur4Zqg4ZuB4Zqx4Zqq4Zur4Zq34ZuW4Zq74Zq54Zum4Zua4Zqz4Zqi4ZuXQW4gcHJlb3N0IHdlcyBvbiBsZW9kZW4sIApMYcidYW1vbiB3YXMgaWhvdGVuIGVyIHN0w65nZXQgw7tmIG1pdCBncsO0emVyIGtyYWZ0IM6kzrcgzrPOu8+Oz4PPg86xIM68zr/PhQrOrc60z4nPg86xzr0gzrXOu867zrfOvc65zrrOriDPhOG9uCDPg8+Azq/PhM65IM+Gz4TPic+HzrnOuuG9uCDPg8+E4b22z4Ig4byAzrzOvM6/z4XOtM654b2yz4IgCs+Ezr/hv6Yg4b2JzrzOrs+Bzr/PhS4g0Jgg0LLQtNCw0LvRjCDQs9C70Y/QtNC10LsuINCf0YDQtdC0INC90LjQvCDRiNC40YDQvtC60L4gCuGDk+GDpuGDmOGDoeGDmOGDlyDhg5Phg5Ag4YOm4YOQ4YOb4YOY4YOXIOGDleGDsOGDruGDlOGDk+GDleGDmOGDk+GDlCDhg5vhg5bhg5jhg6Hhg5Ag4YOU4YOa4YOV4YOQ4YOX4YOQCuGDmeGDoOGDl+GDneGDm+GDkOGDkOGDoeGDkC4g4K6v4K6+4K6u4K6x4K6/4K6o4K+N4K6kIOCuruCviuCutOCuv+CuleCus+Cuv+CusuCvhyDgrqTgrq7grr/grrTgr43grq7gr4rgrrTgrr8KIOCyrOCyviDgsofgsrLgs43gsrLgsr8g4LK44LKC4LKt4LK14LK/4LK44LOBIOCyh+CyguCypuCzhuCyqOCzjeCyqCDgsrngs4Pgsqbgsq/gsqbgsrLgsr8g44GE44KN44Gv44Gr44G744G444GpCuOBoeOCiuOBrOOCi+OCkiDoibLjga/ljILjgbjjgakg5pWj44KK44Gs44KL44KSIApUw7RpIGPDsyB0aOG7gyDEg24gdGjhu6d5IHRpbmggbcOgIGtow7RuZyBo4bqhaSBnw6wuIArmiJHog73lkJ7kuIvnjrvnkoPogIzkuI3kvKTouqvkvZM=".Trim('\n'))));
                        };
        }
    }
}
