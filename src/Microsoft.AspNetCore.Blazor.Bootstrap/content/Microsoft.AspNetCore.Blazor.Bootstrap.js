(function () {
  var functionPrefix = 'Microsoft.AspNetCore.Blazor.Bootstrap.';

  Blazor.registerFunction(functionPrefix + "_init", function (element) {
    console.log(element);
  });

})();
