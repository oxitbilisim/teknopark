﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxit.Common.ViewModels
{
    public interface IBaseViewModel<T>
    {
        T Id { get; set; }
    }
}
