# Инструменты анализа кода

В качестве тестируемой кодовой базы был выбран проект с реализацией решения [задачи об обедающих философах](https://en.wikipedia.org/wiki/Dining_philosophers_problem). Ссылка на репозиторий: https://github.com/Mouad-El-Asri/philosophers

## Helgrind
Запуск производился при помощи следующей команды:
```
valgrind --log-file=helgrind.before --tool=helgrind ./philo 5 800 200 200 7

( ./philo [number_of_philosophers] [time_to_die] [time_to_eat] [time_to_sleep] [number_of_times_each_philosopher_must_eat] )
```
Вывод helgrind об ошибках доступен в [helgrind.before](helrind.before). Инструмент смог обнаружить 37 уязвимостей и ошибок, среди которых также можно увидеть потенциальный deadlock. Если каждый философ попытается взять сначала `rfork`, а затем `lfork`, создавая цикл ожидания - взаимную блокировку.
```
==51105== Use --history-level=approx or =none to gain increased speed, at
==51105== the cost of reduced accuracy of conflicting-access information
==51105== For lists of detected and suppressed errors, rerun with: -s
==51105== ERROR SUMMARY: 37 errors from 9 contexts (suppressed: 245103 from 80)
```
Решение известно - определять порядок захвата вилкок по их индексам - сначала с меньшим, а потом с большим, как и обсуждалось на одном из занятий.
Вывод инструмента для исправленного кода - [helgrind.after](helrind.after). Как можно видеть, количество предупреждений снизилось до 4.
```
==50681== Use --history-level=approx or =none to gain increased speed, at
==50681== the cost of reduced accuracy of conflicting-access information
==50681== For lists of detected and suppressed errors, rerun with: -s
==50681== ERROR SUMMARY: 4 errors from 4 contexts (suppressed: 53028 from 102)
```

## Thread sanitizer (TSAN)
Для использования необходимо было добавить соответствующий флаг компиляции в команду в Makefile.
```
$(NAME): $(OBJS)
	$(CC) $(CFLAGS) $(OBJS) -o $(NAME) -pthread -fsanitize=thread
```
Инструмент вывел сообщения о 4 WARNINGS и 1 ERROR. Среди предупреждений было и обнаруженный в случае helgrind потенциальный deadlock.
```
WARNING: ThreadSanitizer: lock-order-inversion (potential deadlock) (pid=54843)
  Cycle in lock order graph: M4 (0x7b34000000a0) => M0 (0x7b3400000000) => M1 (0x7b3400000028) => M2 (0x7b3400000050) => M3 (0x7b3400000078) => M4
```
После описанного ранее исправления в новом запуске сообщение исчезает.

Также были выведены следующие предупреждения:
```
WARNING: ThreadSanitizer: use of an invalid mutex (e.g. uninitialized or destroyed) (pid=54843)
...
WARNING: ThreadSanitizer: unlock of an unlocked mutex (or by a wrong thread) (pid=54843)
...
```
Однако после некоторого анализа исходного кода показалось, что результат анализа false positive :)
В любом случае, при работе с обоими инструментами сложилось впечатление, что TSAN имеет более описательные и удобные для анализа сообщения о потенциальных ошибках.
