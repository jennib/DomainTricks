﻿using DomainTricks_WPF.Models;
using DomainTricks_WPF.Services;
using Microsoft.Management.Infrastructure;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTricks_WPF.ViewModels
{
    public class MainViewModel
    {
        public MainViewModel(ILogger logger)
        {
            Log.Logger = logger;
            Log.Information("MainViewModel start.");

            // Test the Computer Model.
            Log.Information("Test the ComputerModel.");
            ComputerModel computer = new(logger, Guid.NewGuid());

            // Test the MMIService.
            Log.Information("Test the MMIService.");
            TestMMI(logger);
        }


        // Test the Microsoft Management Infrastructure call.
        async void TestMMI(ILogger logger)
        {
            // Prepare to call MMIService.
            string computerName = "RELIC-PC";
            AuthenticationModel auth = new AuthenticationModel("tuttistudios.com", "jennifer", "password");
            string[] PropertiesArray = { "DriveType" };//{"TotalPhysicalMemory"};
            string ClassName = "Win32_Volume"; //"Win32_ComputerSystem";
            string FilterName = "DriveType=3";

            MMIService mmiService = new MMIService(logger, computerName)
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
                Log.Error(ex, "Exception in TestMMI");
            }

            // Check the Resuylts.
            // The Instances property is of type CimInstance.  
            // It can have multiple Instances and each instance can have multiple Properties.
            if (mmiService.IsError == true)
            {
                Log.Information($"{computerName} returned error: {mmiService.ErrorMessage}");
            }
            else
            {
                Log.Information($"{computerName} returned: {mmiService.Instances.Count()}.");
                foreach (CimInstance instance in mmiService.Instances)
                {
                    Log.Information("");

                    // If we asked for only some properties, then we can query for only those properties.
                    // Also check that PropertiesArray does not contain "*" which is the wildcard search, asks for everything.
                    if (PropertiesArray?.Length > 0 && Array.Exists(PropertiesArray, element => element != "*"))
                    {
                        foreach (string property in PropertiesArray)
                        {
                            Log.Information($"{property} = {instance.CimInstanceProperties[property].Value}");
                        }
                    }
                    else
                    {
                        // Show us all the properties for the instance.
                        foreach (CimProperty property in instance.CimInstanceProperties)
                        {
                            Log.Information($"Name: {property.Name}:{property.Name?.GetType().ToString()} value: {property.Value}:{property.Value?.GetType().ToString()} ");
                        }
                    }
                }
            }
        }
    }
}
