﻿using DomainTricks_WPF.Models;
using Microsoft.Management.Infrastructure;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainTricks_WPF.Services
{
    public class ComputersService
    {

        public ComputersService(ILogger logger)
        {
            Log.Logger = logger;
        }


        public async Task<List<ComputerModel>> GetComputers(string domainPath)
        {
            List<ComputerModel> computers = new();

            // Setup the authentication credentials
            AuthenticationModel auth = new("tuttistudios.com", "jennifer", "password");

            // Get a list of Computers from the Directory.
            ADService adService = new ADService(Log.Logger);
            computers = await adService.GetListOfComputersFromADAsync(domainPath);

            computers = await GetComputers_Win32_LogicalDisks(Log.Logger,computers,auth);

            computers = await GetComputers_Win32_ComputerSystem(Log.Logger, computers, auth);


            return computers;
        }

        private async Task<List<ComputerModel>> GetComputers_Win32_ComputerSystem(ILogger logger, List<ComputerModel> computers, AuthenticationModel auth)
        {
            string[] PropertiesArray = { "*" };//{"TotalPhysicalMemory"};
            string ClassName = "Win32_ComputerSystem"; //"Win32_ComputerSystem";
            string FilterName = "";

            List<ComputerModel> newComputers = new();
            newComputers = await GetListOfComputersWithInstances(Log.Logger, computers, PropertiesArray, ClassName, FilterName, auth);

            //Get the MMI data for each computer.
            //await Task.Run(() =>
            //{
            //    Parallel.ForEach<ComputerModel>(computers, (computer) =>
            //    {
            //        try
            //        {
            //            ComputerModel newComputerWithMMI = GetComputerWithInstances(Log.Logger, computer, PropertiesArray, ClassName, FilterName, auth).Result;
            //            newComputers.Add(newComputerWithMMI);
            //        }
            //        catch (Exception ex)
            //        {
            //            newComputers.Add(computer);
            //            Log.Error($"Error getting MMI data for {computer.Name}.  Error: {ex.Message}");
            //        }
            //    });
            //});

            return newComputers;
        }

        private async Task<List<ComputerModel>> GetComputers_Win32_LogicalDisks(ILogger logger, List<ComputerModel> computers, AuthenticationModel auth)
        {
            string[] PropertiesArray = { "*" };//{"TotalPhysicalMemory"};
            string ClassName = "Win32_LogicalDisk"; //"Win32_ComputerSystem";
            string FilterName = "DriveType=3";

            List<ComputerModel> newComputers = new();

            newComputers = await GetListOfComputersWithInstances(Log.Logger, computers, PropertiesArray, ClassName, FilterName, auth);

            ////Get the MMI data for each computer.
            //await Task.Run(() =>
            //{
            //    Parallel.ForEach<ComputerModel>(computers, (computer) =>
            //    {
            //        try
            //        {
            //            ComputerModel newComputerWithMMI = GetComputerWithInstances(Log.Logger, computer, PropertiesArray, ClassName, FilterName, auth).Result;
            //            newComputers.Add(newComputerWithMMI);
            //        }
            //        catch (Exception ex)
            //        {
            //            newComputers.Add(computer);
            //            Log.Error($"Error getting MMI data for {computer.Name}.  Error: {ex.Message}");
            //        }
            //    });
            //});


            return newComputers;
        }

        private async Task<List<ComputerModel>> GetListOfComputersWithInstances(ILogger logger, 
            List<ComputerModel> computers,
            string[] propertiesArray,
            string className,
            string filterName,
            AuthenticationModel auth)
        {
            List<ComputerModel> newComputers = new();

            //Get the MMI data for each computer.
            await Task.Run(() =>
            {
                Parallel.ForEach<ComputerModel>(computers, (computer) =>
                {
                    try
                    {
                        ComputerModel newComputerWithMMI = GetComputerWithInstances(Log.Logger, computer, propertiesArray, className, filterName, auth).Result;
                        newComputers.Add(newComputerWithMMI);
                    }
                    catch (Exception ex)
                    {
                        newComputers.Add(computer);
                        Log.Error($"Error getting MMI data for {computer.Name}.  Error: {ex.Message}");
                    }
                });
            });

            return newComputers;
        }

        private async Task<ComputerModel> GetComputerWithInstances(ILogger logger,
            ComputerModel computer,
            string[] propertiesArray,
            string className,
            string filterName,
            AuthenticationModel auth)
        {
            // No name, no joy.
            if (string.IsNullOrEmpty(computer.Name))
            {
                throw new Exception("Computer name is null or empty.");
            }

            MMIService mmiService = new(logger, computer.Name)
            {
                Authentication = auth,
                PropertiesArray = propertiesArray,
                ClassName = className,
                FilterName = filterName
            };

            // Call the MMIService .
            try
            {
                await mmiService.Execute();
            }
            catch (Exception ex)
            {
                // Log.Error(ex, "Exception from mmiService: {0}", ex.Message);
                throw;
            }

            // Check the Resuylts.
            // The Instances property is of type CimInstance.  
            // It can have multiple Instances and each instance can have multiple Properties.
            if (mmiService.IsError == true)
            {
                Log.Error($"{computer.Name} returned error: {mmiService.ErrorMessage}");
                throw new Exception($"{computer.Name} returned error: {mmiService.ErrorMessage}");
            }
            else
            {
                // Add to the ComputerMOdel.
                computer.InstancesDictionary.Add(className, mmiService.Instances);


                // Log the results.
                Log.Verbose($"{computer.Name} returned: {mmiService.Instances.Count}.");
                foreach (CimInstance instance in mmiService.Instances)
                {
                    Log.Verbose("");

                    // If we asked for only some properties, then we can query for only those properties.
                    // Also check that PropertiesArray does not contain "*" which is the wildcard search, asks for everything.
                    if (propertiesArray?.Length > 0 && Array.Exists(propertiesArray, element => element != "*"))
                    {
                        foreach (string property in propertiesArray)
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
                return computer;
            }
        }
    }
}
