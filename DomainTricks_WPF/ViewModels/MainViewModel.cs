﻿using DomainTricks_WPF.Models;
using DomainTricks_WPF.Services;
using Microsoft.Management.Infrastructure;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTricks_WPF.ViewModels;

public class MainViewModel
{
    public string? Title { get; set; } = "Domain Tricks";
    public MainViewModel(ILogger logger)
    {


        Log.Logger = logger;
        Log.Information("MainViewModel start.");

        // Test Timer
        //TestTimer(logger);

        // Test the ComputersService
        Log.Information("Testing the ComputersService.");
        TestComputersService(logger);

        //// Test the Computer Model.
        //Log.Information("Test the ComputerModel.");
        //ComputerModel computer = new("MyCompuyter", logger);

        //// Test the Domain Service
        //Log.Information("Test the DomainService.");
        //TestDomainService(logger);

        //// Test the Direcotry Search
        //Log.Information("Test the Directory Search.");
        //TestADSearcher(logger);

        //// Test the MMIService.
        //Log.Information("Test the MMIService.");
        //TestMMI(logger, computer);


    }

    public void MenuClickedCommand(object sender)
    {
        
    }

    // Test the ComputersService.
    public async void TestComputersService(ILogger logger)
    {
        DomainService domainService = new(logger);
        string domainPath = await DomainService.GetCurrentDomainPathAsync();

        ComputersService computers = new(logger);
        List<ComputerModel> computersList = await computers.GetComputers(domainPath);
        foreach (ComputerModel computer in computersList)
        {
            Log.Information($"Computer: {computer.Name}: {computer.InstancesDictionary.Count} instances.  Last seen {computer.DateLastSeen?.ToString("f") }");
        }
    }

    // Test the Timer in BackgroundTask
    public async void TestTimer(ILogger logger)
    {
        Log.Information("Test the Timer in BackgroundTask.");
        BackgroundTask task = new BackgroundTask(TimeSpan.FromMilliseconds(1000), logger);

        task.Start();

        await Task.Delay(TimeSpan.FromSeconds(10));
         await task.StopAsync();
    }

    // Test the Domain Service call
    async void TestDomainService(ILogger logger)
    {
        DomainService domainService = new(logger);
        string domainPath = await DomainService.GetCurrentDomainPathAsync();
        string domainName = DomainService.DomainNameFromLDAPPath(domainPath);
        Log.Information($"Domain path: {domainPath} name: {domainName}");

    }

    // Test the Active Directory Searcher;
    async void TestADSearcher(ILogger logger)
    {

        ADService adService = new(logger);
        List<ComputerModel> computerModels = await adService.GetListOfComputersFromADAsync(@"LDAP://DC=tuttistudios,DC=com");
        Log.Information($"computerModels has {computerModels.Count()} computers.");

    }

    // Test the Microsoft Management Infrastructure call.
    async void TestMMI(ILogger logger, ComputerModel computer)
    {
        // Prepare to call MMIService.
        string computerName = "RELIC-PC";
        AuthenticationModel auth = new("tuttistudios.com", "jennifer", "password");
        // AuthenticationModel auth = new();
        string[] PropertiesArray = { "*" };//{"TotalPhysicalMemory"};
        string ClassName = "Win32_Volume"; //"Win32_ComputerSystem";
        string FilterName = "";

        MMIService mmiService = new(logger, computerName)
        {
            Authentication = auth,
            PropertiesArray = PropertiesArray,
            ClassName = ClassName,
            FilterName = FilterName
        };

        // Call the MMIService .
        try
        {
            await mmiService.Execute();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception in TestMMI: {0}", ex.Message);
        }

        // Check the Resuylts.
        // The Instances property is of type CimInstance.  
        // It can have multiple Instances and each instance can have multiple Properties.
        if (mmiService.IsError == true)
        {
            Log.Error($"{computerName} returned error: {mmiService.ErrorMessage}");
        }
        else
        {
            // Add to the ComputerMOdel.
            computer.InstancesDictionary.Add(ClassName, mmiService.Instances);

            Log.Verbose($"{computerName} returned: {mmiService.Instances.Count}.");
            foreach (CimInstance instance in mmiService.Instances)
            {
                Log.Verbose("");

                // If we asked for only some properties, then we can query for only those properties.
                // Also check that PropertiesArray does not contain "*" which is the wildcard search, asks for everything.
                if (PropertiesArray?.Length > 0 && Array.Exists(PropertiesArray, element => element != "*"))
                {
                    foreach (string property in PropertiesArray)
                    {
                        Log.Verbose($"{property} = {instance.CimInstanceProperties[property].Value}");
                    }
                }
                else
                {
                    // Show us all the properties for the instance.
                    foreach (CimProperty property in instance.CimInstanceProperties)
                    {
                        Log.Verbose($"Name: {property.Name}:{property.Name?.GetType().ToString()} value: {property.Value}:{property.Value?.GetType().ToString()} ");
                    }
                }
            }
        }
    }
}


