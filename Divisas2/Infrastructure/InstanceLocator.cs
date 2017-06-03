using System;
using Divisas2.ViewModels;

namespace Divisas2.Infrastructure
{
    public class InstanceLocator
	{
		public MainViewModel Main { get; set; }

		public InstanceLocator()
		{
			Main = new MainViewModel();
		}
	}
}
