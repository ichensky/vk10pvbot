# vk10pvbot

Эта программа является ботом для игры 10 пв. Её цель свести к минимум действия ведущего игры. 

## Правила Игры

![rules](https://github.com/ichensky/vk10pvbot/blob/master/doc/rules.jpg)

## Системные требования 

1. Windows XP, Vista, 7, 8.1, 10 и выше. 
2. Microsoft .NET Framework 4.5.2 . 
   Скачать можно тут: 
   
   https://www.microsoft.com/en-us/download/details.aspx?id=42643&desc=dotnet452


## Запуск бота

1. Создать приложение в вконтакте.  
2. Отредактировать настройки для бота(файл). 
3. Запустить бот.

## Создать приложени в вконтакте

Бот исспользует vk.com api , потому чтобы он заработал на нужно создать приложение в вконтакте и скопировать от-туда app_id. 

1. Перейдите по ссылке, и создайте приложение: [тут ссылка на вк для создания приложения]
2. Заполните эти поля: [тут картинка на настройки приложения]

## Настройка бота

1. Создайте на компьютере текстовый файл с именем напр. `file.txt`, 

   Как пример можно скачать этот файл(так же его можно найти в архивет в ботом): [скачать](https://raw.githubusercontent.com/ichensky/vk10pvbot/master/file.txt)


2. Впишите туда:

```json
{
    "app_id"      :     00000000,
    "email"       :    "***@***",
    "password"    :    "******",
    "chatname"    :    "10 поводов"
}
```
 где
  
* app_id   - это app_id созданого 
* email    - логин в вк
* password - пароль от вк
* chatname - название чата в котором будет работать бот(можно не полное, а первых несколько букв)

3. Откройте, в блокноте файл `start.bat` (он дожен быть в архиве с ботом) и впишите туда: 

```sh
vk10pvbot.exe "C:\path_to_file\file.txt"
```
где 

* "C:\path_to_file\file.txt" - это полный путь к файлу с настройками, который создали ранее.

## Запуск бота

1. Запустите файл start.bat [тут картинка запущенного консольного окна]

## Комманды бота
...


