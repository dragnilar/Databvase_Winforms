﻿using System.Collections.Generic;
using Databvase_Winforms.Models;

namespace LWSqlQueryTool_Winforms.Models
{
    public class SQLDatabase
    {
        public string DataBaseName { get; set; }
        public List<SQLColumn> Columns { get; set; }
        public List<SQLTable> Tables { get; set; }
    }
}