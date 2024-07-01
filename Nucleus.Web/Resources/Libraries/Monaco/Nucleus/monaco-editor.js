/*! monaco-editor | Provides a jQuery plugin wrapper for the Monaco editor for use in admin pages in Nucleus CMS.  (c) Inventua Pty Ptd.  www.nucleus-cms.com */
var require = { paths: { vs: 'Resources/Libraries/Monaco/0.50.0/min/vs' } };
(function ($)
{
  jQuery.fn.MonacoEditor = function (conf)
  {
    // Config for plugin
    var config = jQuery.extend({
    }, conf);

    var hasRegisteredCompletionItemProvider = false;

    return this.each(function (index, value)
    {
      if (typeof monaco === 'undefined')
      {
        return;
      }
      
      if (!hasRegisteredCompletionItemProvider)
      {
        if (conf.model)
        {
          _registerMonacoCompletionItemProvider();
        }
        hasRegisteredCompletionItemProvider = true;
      }

      this._monacoEditor = monaco.editor.create(value,
        {
          value: [jQuery(config.linkedElement).val()].join('\n'),
          language: 'razor',
          automaticLayout: true,
          lineNumbersWidth: 3,
          useShadowDOM: true,
          minimap: { enabled: false },
        });

      if (typeof conf.linkedElement !== 'undefined')
      {
        this._monacoEditor.linkedElement = jQuery(conf.linkedElement);

        this._monacoEditor.getModel().onDidChangeContent((event) =>
        {
          if (this._monacoEditor.linkedElement.length !== 0)
          {
            this._monacoEditor.linkedElement.val(this._monacoEditor.getValue());
          }
        });

        if (this._monacoEditor.linkedElement.length !== 0)
        {
          this._monacoEditor.linkedElement.hide();
        }
      }
    });

    // private functions
    function _registerMonacoCompletionItemProvider()
    {
      if (typeof window.monaco_completionItemProviderInstance !== 'undefined' && window.monaco_completionItemProviderInstance !== null)
      {
        window.monaco_completionItemProviderInstance.dispose()
      }

      window.monaco_completionItemProviderInstance = monaco.languages.registerCompletionItemProvider("razor", {
        triggerCharacters: ['.'],
        provideCompletionItems: function (model, position)
        {          
          // find out if we are completing a property in the 'dependencies' object.
          var textUntilPosition = model.getValueInRange({
            startLineNumber: 1,
            startColumn: 1,
            endLineNumber: position.lineNumber,
            endColumn: position.column,
          });
          var lines = textUntilPosition.split('\n');
          var match = lines[lines.length - 1].match('@Model[\?]{0,1}[\.]{1}(?<expression>[A-Za-z0-9_.]*)[\s(]*(?<parameters>[A-Za-z0-9._]*)$');
          if (!match)
          {
            return { suggestions: [] };
          }
          var word = model.getWordUntilPosition(position);
          var range =
          {
            startLineNumber: position.lineNumber,
            endLineNumber: position.lineNumber,
            startColumn: word.startColumn,
            endColumn: word.endColumn
          };
          var expressions = match.groups.expression.split('.').filter(element => element);
          var parameters = match.groups.parameters.split('.').filter(element => element).map(parm => '#' + parm);
          return {
            suggestions:
              parameters.length === 0 ? _createDependencyProposals(conf.model, expressions, false, range):_createDependencyProposals(conf.model, parameters, true, range)
          };
        },
      });
    }

    function _createDependencyProposals(data, expression, exactMatch, range)
    {
      var results = [];
      var classModel;
      
      if (expression.length === 0)
      {
        if (exactMatch) return results;
        classModel = data;
      }
      else
      {
        classModel = data[expression[0]];

        if (typeof classModel === 'undefined' || classModel === null)
        {
          // calls to functions end in (), and our model generator code will add _(p1,p2...) to the name to make it unique, which means that the
          // 'data[expression[0]' code above does not work in these cases.  Look for a match less strictly
          var filteredExpression = expression[0].match('[A-Za-z0-9]*').toString();
          for (var propertyName in data)
          {
            if (propertyName.match('[A-Za-z0-9]*').toString() === filteredExpression)
            {
              classModel = data[propertyName];
            }
          }
        }
      }

      if (typeof classModel !== 'undefined' && classModel !== null)
      {
        if (expression.length === 0)
        {
          for (var propertyName in classModel)
          {

            if (expression.length > 0 || !propertyName.startsWith('#'))
            {
              var propertyInfo = classModel[propertyName];
            
              result =
              {
                label: (propertyInfo.label ?? propertyName),
                kind: (propertyInfo.kind ?? monaco.languages.CompletionItemKind.Property),
                documentation: (propertyInfo.documentation ?? ''),
                insertText: (propertyInfo.code ?? propertyName),
                range: range
              }
              results[results.length] = result;
            }
          }         
        }
        else if (typeof expression.shift() !== 'undefined')
        {
          if (typeof classModel.properties !== 'undefined' && classModel.properties !== null)
          {
            results = _createDependencyProposals(classModel.properties, expression, false, range);
          }
          if (typeof classModel.functions !== 'undefined' && classModel.functions !== null)
          {
            results = results.concat(_createDependencyProposals(classModel.functions, expression, false, range));
          }
        }
      }
      return results;
    }
  }
})(jQuery);



