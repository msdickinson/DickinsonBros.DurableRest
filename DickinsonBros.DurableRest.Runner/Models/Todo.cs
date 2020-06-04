using System;
using System.Collections.Generic;
using System.Text;

namespace DickinsonBros.DurableRest.Runner.Models
{
    public class Todo
    {
        public int UserId  {get; set;}
        public int Id  {get; set;}
        public string Title { get; set;}
        public bool Completed { get; set; }
    }
}
