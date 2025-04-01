" Vim syntax file
" Language:     Visual Basic
" Maintainer:	Joseph Guenther <joseph@codingcool.ca>
" 	Updated to support VB.NET, does not support VB6
" Former Maintainer:    Doug Kearns <dougkearns@gmail.com>
" Former Maintainer:    Tim Chase <vb.vim@tim.thechases.com>
" Former Maintainer:    Robert M. Cortopassi <cortopar@mindspring.com>
"       (tried multiple times to contact, but email bounced)
" Last Change:
"   2021 Nov 26  Incorporated additions from Doug Kearns
"   2005 May 25  Synched with work by Thomas Barthel
"   2004 May 30  Added a few keywords

" This was thrown together after seeing numerous requests on the
" VIM and VIM-DEV mailing lists.  It is by no means complete.
" Send comments, suggestions and requests to the maintainer.

" quit when a syntax file was already loaded
if exists("b:current_syntax")
        finish
endif

" VB is case insensitive
syn case ignore

syn keyword vbConditional If Then ElseIf Else Select Case

syn keyword vbOperator AddressOf And ByRef ByVal In
syn keyword vbOperator Is Like Mod Not Or To Xor

syn match vbOperator "[()+.,\-/*=&]"
syn match vbOperator "[<>]=\="
syn match vbOperator "<>"
syn match vbOperator "\s\+_$"

syn keyword vbBoolean  True False
syn keyword vbConst Nothing

syn keyword vbRepeat Do For ForEach Loop Next
syn keyword vbRepeat Step To Until Wend While

syn keyword vbFunction Array CBool CByte
syn keyword vbFunction CCur CDate CDbl CInt CLng CSng CStr CVDate CVErr

syn keyword vbStatement Alias As Base Begin Call
syn keyword vbStatement Const Date Declare Dim Do
syn keyword vbStatement Each ElseIf End Enum Error Event Exit
syn keyword vbStatement Explicit For ForEach Function Get
syn keyword vbStatement GoTo Implements Let Lib
syn keyword vbStatement Lock Loop Next On OnError
syn keyword vbStatement Option Private Property Public
syn keyword vbStatement RaiseEvent Redim Resume
syn keyword vbStatement Return
syn keyword vbStatement Set Static Step Sub
syn keyword vbStatement Until Wend While With

syn keyword vbKeyword As Binary ByRef ByVal Date Error Friend Get
syn keyword vbKeyword Is Lock Me New Nothing On
syn keyword vbKeyword Option Optional ParamArray Private Property
syn keyword vbKeyword Set Static String WithEvents

" VB.NET keywords
syn keyword vbKeyword Class Module Imports Overridable Protected Overrides
syn keyword vbKeyword OrElse AndAlso Interface Namespace ReadOnly AddHandler
syn keyword vbKeyword AddressOf RemoveHandler CType DirectCast CDec Handles
syn keyword vbKeyword Async Await MyBase IsNot Of Try Catch Finally Throw
syn keyword vbKeyword Shared Not Using Shadows MustInherit MustOverride Inherits
syn keyword vbKeyword Operator Iterator Narrowing Widening Partial NotInheritable
syn keyword vbKeyword NotOverridable GetType

syn keyword vbTodo contained    TODO

"Datatypes
syn keyword vbTypes Boolean Byte Currency Date Decimal Double Empty
syn keyword vbTypes Integer Long Object Single String Variant

"Numbers
"integer number, or floating point number without a dot.
syn match vbNumber "\<\d\+\>"
"floating point number, with dot
syn match vbNumber "\<\d\+\.\d*\>"
"floating point number, starting with a dot
syn match vbNumber "\.\d\+\>"
"syn match  vbNumber            "{[[:xdigit:]-]\+}\|&[hH][[:xdigit:]]\+&"
"syn match  vbNumber            ":[[:xdigit:]]\+"
"syn match  vbNumber            "[-+]\=\<\d\+\>"
syn match  vbFloat              "[-+]\=\<\d\+[eE][\-+]\=\d\+"
syn match  vbFloat              "[-+]\=\<\d\+\.\d*\([eE][\-+]\=\d\+\)\="
syn match  vbFloat              "[-+]\=\<\.\d\+\([eE][\-+]\=\d\+\)\="

" String and Character constants
syn region  vbString		start=+"+  end=+"\|$+
syn region  vbComment		start="\(^\|\s\)REM\s" end="$" contains=vbTodo
syn region  vbComment		start="\(^\|\s\)\'"   end="$" contains=vbTodo
syn match   vbLineLabel		"^\h\w\+:"
syn match   vbLineNumber	"^\d\+\(:\|\s\|$\)"
syn match   vbTypeSpecifier  "\<\a\w*[@\$%&!#]"ms=s+1
syn match   vbTypeSpecifier  "#[a-zA-Z0-9]"me=e-1
" Conditional Compilation
syn match  vbPreProc "^#const\>"
syn region vbPreProc matchgroup=PreProc start="^#if\>"     end="\<then\>" transparent contains=TOP
syn region vbPreProc matchgroup=PreProc start="^#elseif\>" end="\<then\>" transparent contains=TOP
syn match  vbPreProc "^#else\>"
syn match  vbPreProc "^#end\s*if\>"

" Define the default highlighting.
" Only when an item doesn't have highlighting yet

hi def link vbBoolean           Boolean
hi def link vbLineNumber        Comment
hi def link vbLineLabel         Comment
hi def link vbComment           Comment
hi def link vbConditional       Conditional
hi def link vbConst             Constant
hi def link vbError             Error
hi def link vbFunction          Identifier
hi def link vbIdentifier        Identifier
hi def link vbNumber            Number
hi def link vbFloat             Float
hi def link vbMethods           PreProc
hi def link vbOperator          Operator
hi def link vbRepeat            Repeat
hi def link vbString            String
hi def link vbStatement         Statement
hi def link vbKeyword           Statement
hi def link vbEvents            Special
hi def link vbTodo              Todo
hi def link vbTypes             Type
hi def link vbTypeSpecifier     Type
hi def link vbPreProc           PreProc

let b:current_syntax = "vbnet"

" vim: ts=8
