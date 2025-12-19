using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Input;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ComplexVirtualizationPageViewModel : ViewModelBase
    {
        private bool _enableVirtualization;

        public ComplexVirtualizationPageViewModel()
        {
            // Create a mixed collection of different item types
            _items = new List<object>();
            var random = new Random(42); // Fixed seed for consistent data

            for (int i = 0; i < 5000; i++)
            {
                // Randomly distribute items across 4 types (weighted distribution)
                // This creates a more realistic scenario where types aren't evenly distributed
                var type = random.Next(100) switch
                {
                    < 30 => 0,  // 30% PersonItem
                    < 55 => 1,  // 25% TaskItem
                    < 80 => 2,  // 25% ProductItem
                    _ => 3      // 20% PhotoItem
                };
                switch (type)
                {
                    case 0:
                        // Create highly variable bio lengths: 0-10 sentences with weighted distribution
                        var bioLength = random.Next(11) switch
                        {
                            0 => 0,                    // 9% - No bio
                            1 or 2 => 1,               // 18% - Very short (1 line)
                            3 or 4 or 5 => random.Next(2, 4),   // 27% - Short (2-3 lines)
                            6 or 7 => random.Next(4, 6),        // 18% - Medium (4-5 lines)
                            8 or 9 => random.Next(6, 9),        // 18% - Long (6-8 lines)
                            _ => random.Next(9, 12)             // 9% - Very long (9-11 lines)
                        };
                        var bio = bioLength > 0 ? string.Join(" ", Enumerable.Range(0, bioLength).Select(_ => SampleText[random.Next(SampleText.Length)])) : "";
                        var hasSkills = random.Next(2) == 0;
                        var skillCount = hasSkills ? random.Next(2, 10) : 0;  // More skills for variance
                        var skills = new List<string>();
                        if (hasSkills)
                        {
                            for (int j = 0; j < skillCount; j++)
                            {
                                skills.Add(Skills[random.Next(Skills.Length)]);
                            }
                        }
                        _items.Add(new PersonItem
                        {
                            Id = i,
                            Name = $"{FirstNames[random.Next(FirstNames.Length)]} {LastNames[random.Next(LastNames.Length)]}",
                            Email = $"person{i}@{Domains[random.Next(Domains.Length)]}",
                            Department = Departments[random.Next(Departments.Length)],
                            Bio = bio,
                            PhoneNumber = $"+1 {random.Next(200, 999)}-{random.Next(100, 999)}-{random.Next(1000, 9999)}",
                            YearsExperience = random.Next(0, 30),
                            Skills = skills,
                            IsActive = random.Next(2) == 0,
                            LastActivity = DateTime.Now.AddDays(-random.Next(0, 90))
                        });
                        break;
                    case 1:
                        // Variable task descriptions: 1-10 sentences
                        var descLength = random.Next(11) switch
                        {
                            0 or 1 => 1,                        // 18% - Very short
                            2 or 3 or 4 => random.Next(2, 4),   // 27% - Short
                            5 or 6 or 7 => random.Next(4, 7),   // 27% - Medium
                            8 or 9 => random.Next(7, 10),       // 18% - Long
                            _ => random.Next(10, 13)            // 9% - Very long
                        };
                        var description = string.Join(" ", Enumerable.Range(0, descLength).Select(_ => SampleText[random.Next(SampleText.Length)]));
                        var hasSubtasks = random.Next(3) == 0; // 33% chance
                        var subtaskCount = hasSubtasks ? random.Next(2, 8) : 0;  // More subtasks
                        var subtasks = new List<string>();
                        if (hasSubtasks)
                        {
                            for (int j = 0; j < subtaskCount; j++)
                            {
                                subtasks.Add($"{TaskActions[random.Next(TaskActions.Length)]} {TaskObjects[random.Next(TaskObjects.Length)]}");
                            }
                        }
                        _items.Add(new TaskItem
                        {
                            Id = i,
                            Title = $"{TaskActions[random.Next(TaskActions.Length)]} {TaskObjects[random.Next(TaskObjects.Length)]}",
                            Description = description,
                            IsCompleted = random.Next(2) == 0,
                            Priority = (Priority)random.Next(3),
                            DueDate = DateTime.Now.AddDays(random.Next(-10, 30)),
                            Assignee = $"{FirstNames[random.Next(FirstNames.Length)]} {LastNames[random.Next(LastNames.Length)]}",
                            Subtasks = subtasks,
                            ProgressPercentage = random.Next(0, 101)
                        });
                        break;
                    case 2:
                        var tagCount = random.Next(1, 12); // 1-11 tags for more variance
                        var tags = new List<string>();
                        for (int j = 0; j < tagCount; j++)
                        {
                            var tag = AllTags[random.Next(AllTags.Length)];
                            if (!tags.Contains(tag)) tags.Add(tag);
                        }
                        // Variable product descriptions: 0-8 sentences
                        var productDescLength = random.Next(9) switch
                        {
                            0 => 0,                             // 11% - No description
                            1 or 2 => 1,                        // 22% - Very short
                            3 or 4 => random.Next(2, 4),        // 22% - Short
                            5 or 6 => random.Next(4, 6),        // 22% - Medium
                            7 or 8 => random.Next(6, 9)         // 22% - Long
                        };
                        var productDesc = productDescLength > 0 ? string.Join(" ", Enumerable.Range(0, productDescLength).Select(_ => SampleText[random.Next(SampleText.Length)])) : "";
                        _items.Add(new ProductItem
                        {
                            Id = i,
                            ProductName = $"{Adjectives[random.Next(Adjectives.Length)]} {ProductTypes[random.Next(ProductTypes.Length)]}",
                            Price = random.Next(10, 1000),
                            Tags = tags,
                            InStock = random.Next(2) == 0,
                            Rating = random.Next(1, 6),
                            ReviewCount = random.Next(0, 5000),
                            Description = productDesc,
                            Category = ProductCategories[random.Next(ProductCategories.Length)],
                            Discount = random.Next(4) == 0 ? random.Next(5, 50) : 0 // 25% chance of discount
                        });
                        break;
                    case 3:
                        // Variable photo captions: 0-8 sentences
                        var captionLength = random.Next(9) switch
                        {
                            0 => 0,                             // 11% - No caption
                            1 or 2 => 1,                        // 22% - Very short
                            3 or 4 => random.Next(2, 4),        // 22% - Short
                            5 or 6 => random.Next(4, 6),        // 22% - Medium
                            7 or 8 => random.Next(6, 9)         // 22% - Long
                        };
                        var caption = captionLength > 0 ? string.Join(" ", Enumerable.Range(0, captionLength).Select(_ => SampleText[random.Next(SampleText.Length)])) : "";
                        var hasComments = random.Next(3) == 0; // 33% chance
                        var commentCount = hasComments ? random.Next(1, 7) : 0;  // More comments for variance
                        var comments = new List<string>();
                        if (hasComments)
                        {
                            for (int j = 0; j < commentCount; j++)
                            {
                                comments.Add($"{FirstNames[random.Next(FirstNames.Length)]}: {SampleText[random.Next(SampleText.Length)]}");
                            }
                        }
                        _items.Add(new PhotoItem
                        {
                            Id = i,
                            Title = $"{PhotoAdjectives[random.Next(PhotoAdjectives.Length)]} {PhotoSubjects[random.Next(PhotoSubjects.Length)]}",
                            Location = Locations[random.Next(Locations.Length)],
                            ImageUrl = $"avares://ControlCatalog/Assets/avalonia-32.png",
                            Caption = caption,
                            Likes = random.Next(0, 10000),
                            DateTaken = DateTime.Now.AddDays(-random.Next(0, 1000)),
                            Comments = comments,
                            CameraModel = random.Next(2) == 0 ? CameraModels[random.Next(CameraModels.Length)] : "",
                            IsPublic = random.Next(2) == 0
                        });
                        break;
                }
            }

            Items = new BulkObservableCollection<object>(_items);
        }

        private bool _many = false;
        private readonly List<object> _items;

        public void RecycleList()
        {
            Items.BeginUpdate();
            Items.Clear();
            
            if(_many)
            {    
                foreach(var elt in _items)
                    Items.Add(elt);
            }
            else
            {
                Items.Add(_items.First());
            }

            _many = !_many;

            Items.EndUpdate();
        }
        
        public BulkObservableCollection<object> Items { get; }

        public bool EnableVirtualization
        {
            get => _enableVirtualization;
            set => this.RaiseAndSetIfChanged(ref _enableVirtualization, value);
        }

        private static readonly string[] Departments = { "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations", "R&D", "Customer Support" };
        private static readonly string[] Locations = { "New York", "London", "Tokyo", "Paris", "Sydney", "Berlin", "Singapore", "Toronto", "Dubai", "Mumbai" };
        private static readonly string[] AllTags = { "Featured", "New", "Sale", "Popular", "Limited", "Premium", "Eco-Friendly", "Best Seller", "Trending", "Exclusive", "Organic", "Handmade" };
        private static readonly string[] FirstNames = { "Alex", "Jordan", "Morgan", "Taylor", "Casey", "Riley", "Avery", "Quinn", "Sage", "Rowan" };
        private static readonly string[] LastNames = { "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
        private static readonly string[] Domains = { "example.com", "company.com", "business.org", "enterprise.net" };
        private static readonly string[] TaskActions = { "Review", "Update", "Complete", "Investigate", "Fix", "Implement", "Design", "Test", "Deploy", "Analyze" };
        private static readonly string[] TaskObjects = { "Documentation", "Bug Report", "Feature Request", "Security Issue", "Performance", "UI/UX", "Database", "API", "Integration", "Configuration" };
        private static readonly string[] Adjectives = { "Premium", "Deluxe", "Professional", "Standard", "Economy", "Elite", "Advanced", "Basic", "Ultimate", "Classic" };
        private static readonly string[] ProductTypes = { "Widget", "Gadget", "Tool", "Device", "Kit", "System", "Module", "Component", "Package", "Bundle" };
        private static readonly string[] PhotoAdjectives = { "Stunning", "Beautiful", "Majestic", "Serene", "Vibrant", "Peaceful", "Dramatic", "Colorful", "Minimalist", "Abstract" };
        private static readonly string[] PhotoSubjects = { "Sunset", "Landscape", "Portrait", "Architecture", "Wildlife", "Street Scene", "Nature", "Cityscape", "Ocean View", "Mountain" };
        private static readonly string[] SampleText =
        {
            "Lorem ipsum dolor sit amet consectetur adipiscing elit.",
            "Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
            "Ut enim ad minim veniam quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
            "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
            "Excepteur sint occaecat cupidatat non proident sunt in culpa qui officia deserunt mollit anim id est laborum.",
            "Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas vestibulum tortor quam feugiat vitae ultricies eget tempor sit amet ante.",
            "Donec pretium vulputate sapien nec sagittis aliquam malesuada bibendum arcu vitae elementum curabitur vitae nunc sed velit dignissim sodales ut eu sem integer vitae justo.",
            "Mauris in aliquam sem fringilla ut morbi tincidunt augue interdum velit euismod in pellentesque massa placerat duis ultricies lacus sed turpis tincidunt id aliquet risus feugiat.",
            "Viverra ipsum nunc aliquet bibendum enim facilisis gravida neque convallis a cras semper auctor neque vitae tempus quam pellentesque nec nam aliquam sem et tortor consequat.",
            "Faucibus turpis in eu mi bibendum neque egestas congue quisque egestas diam in arcu cursus euismod quis viverra nibh cras pulvinar mattis nunc sed blandit libero volutpat.",
            "Short text.",
            "A",
            "Brief.",
            "This is a medium-length sentence that spans multiple words but stays relatively concise.",
            "An extraordinarily long sentence that continues on and on with excessive verbosity describing mundane details in an unnecessarily elaborate manner using superfluous adjectives and redundant phrases to maximize the character count and ensure text wrapping across multiple lines when displayed in a constrained width container."
        };
        private static readonly string[] Skills = { "C#", "Python", "JavaScript", "React", "Angular", "Vue", "Node.js", "Docker", "Kubernetes", "AWS", "Azure", "SQL", "MongoDB", "Git" };
        private static readonly string[] ProductCategories = { "Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys", "Food & Beverage" };
        private static readonly string[] CameraModels = { "Canon EOS R5", "Nikon Z9", "Sony A7R V", "Fujifilm X-T5", "iPhone 14 Pro" };
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }

    public class PersonItem : ViewModelBase
    {
        private string _name = string.Empty;

        public int Id { get; set; }

        public string Name
        {
            get => _name;
            set => this.RaiseAndSetIfChanged(ref _name, value);
        }

        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public int YearsExperience { get; set; }
        public List<string> Skills { get; set; } = new();
        public bool IsActive { get; set; }
        public DateTime LastActivity { get; set; }
    }

    public class TaskItem : ViewModelBase
    {
        private bool _isCompleted;

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public bool IsCompleted
        {
            get => _isCompleted;
            set => this.RaiseAndSetIfChanged(ref _isCompleted, value);
        }

        public Priority Priority { get; set; }
        public DateTime DueDate { get; set; }
        public string Assignee { get; set; } = string.Empty;
        public List<string> Subtasks { get; set; } = new();
        public int ProgressPercentage { get; set; }
    }

    public class ProductItem
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<string> Tags { get; set; } = new();
        public bool InStock { get; set; }
        public int Rating { get; set; }
        public int ReviewCount { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Discount { get; set; }
    }

    public class PhotoItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Caption { get; set; } = string.Empty;
        public int Likes { get; set; }
        public DateTime DateTaken { get; set; }
        public List<string> Comments { get; set; } = new();
        public string CameraModel { get; set; } = string.Empty;
        public bool IsPublic { get; set; }
    }

    /// <summary>
    /// Represents a dynamic data collection that provides notifications when items get added, removed, or when the whole list is refreshed.
    /// Supports suspending notifications during bulk operations via BeginUpdate/EndUpdate.
    /// </summary>
    /// <typeparam name="T">The type of elements in the collection.</typeparam>
    public class BulkObservableCollection<T> : ObservableCollection<T>
    {
        private bool _suppressNotification;

        public BulkObservableCollection()
        {
            
        }

        public BulkObservableCollection(IEnumerable<T> enumerable)
        :base(enumerable)
        {
            
        }
        
        /// <summary>
        /// Begins a bulk update operation. CollectionChanged events are suppressed until EndUpdate is called.
        /// </summary>
        public void BeginUpdate()
        {
            _suppressNotification = true;
        }

        /// <summary>
        /// Ends a bulk update operation and raises a Reset notification to refresh all listeners.
        /// </summary>
        public void EndUpdate()
        {
            _suppressNotification = false;
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        /// <summary>
        /// Raises the CollectionChanged event with the provided arguments.
        /// Events are suppressed when a bulk update is in progress.
        /// </summary>
        /// <param name="e">Arguments that describe the change.</param>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (!_suppressNotification)
                base.OnCollectionChanged(e);
        }
    }
}
