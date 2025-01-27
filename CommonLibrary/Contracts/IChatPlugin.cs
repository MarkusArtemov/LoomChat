﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace De.Hsfl.LoomChat.Common.Contracts
{
    public interface IChatPlugin
    {
        string Name { get; }
        Task Initialize();
    }
}