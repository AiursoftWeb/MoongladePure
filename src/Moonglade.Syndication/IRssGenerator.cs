﻿namespace MoongladePure.Syndication;

public interface IRssGenerator
{
    Task<string> WriteRssAsync();
}