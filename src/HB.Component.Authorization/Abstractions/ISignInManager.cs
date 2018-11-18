﻿using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HB.Component.Authorization.Abstractions
{
    public interface ISignInManager
    {
        Task<SignInResult> SignInAsync(SignInContext context);

        Task SignOutAsync(string userTokenIdentifier);
    }
}
