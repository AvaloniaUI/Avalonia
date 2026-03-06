using System;
using Avalonia.Controls;

namespace IntegrationTestApp.Models;

internal record Page(string Name, Func<Control> CreateContent);
