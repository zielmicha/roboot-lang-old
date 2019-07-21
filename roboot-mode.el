(defconst roboot-mode-syntax-table
  (let ((table (make-syntax-table)))
    ;; ' is a string delimiter
    (modify-syntax-entry ?' "\"" table)
    ;; " is a string delimiter too
    (modify-syntax-entry ?\" "\"" table)
    (modify-syntax-entry ?\\ "\\" table)

    (modify-syntax-entry ?# "< b" table)
    (modify-syntax-entry ?\n "> b" table)

    (modify-syntax-entry ?\( "((")
    (modify-syntax-entry ?\) "))")

    (modify-syntax-entry ?\{ "({")
    (modify-syntax-entry ?\} ")}")

    (modify-syntax-entry ?\[ "([")
    (modify-syntax-entry ?\] ")]")

    table))

(setq roboot-highlights
      '(("\\b\\(module\\|if\\|else\\|fun\\|let\\|letmut\\|val\\|methodbase\\|while\\|struct\\|return\\|import\\|include\\|type\\|coercion\\|match\\)\\b" . font-lock-keyword-face)))

(setq roboot-indent-offset 2)

(defun roboot-indent-line ()
  "Indent current line for `roboot-mode'."
  (interactive)
  (let ((indent-col roboot-indent-offset))
    (save-excursion
      (re-search-backward "[^ \t\r\n]" nil t)
      (when (looking-at ";")
        (setq indent-col (- indent-col roboot-indent-offset))))

    (save-excursion
      (beginning-of-line)
      (condition-case nil
          (while t
            (backward-up-list 1)
            (when (looking-at "[[{(]")
              (setq indent-col (+ indent-col roboot-indent-offset))))
        (error nil)))
    (save-excursion
      (back-to-indentation)
      (when (and (looking-at "[]})]") (>= indent-col roboot-indent-offset))
        (setq indent-col (- indent-col roboot-indent-offset))))
    (indent-line-to indent-col)))

(define-derived-mode roboot-mode prog-mode "Roboot mode"
  :syntax-table roboot-mode-syntax-table
  (setq font-lock-defaults '(roboot-highlights))

  (set (make-local-variable 'indent-line-function) 'roboot-indent-line)
  (set (make-local-variable 'comment-start) "# ")
  (set (make-local-variable 'comment-start-skip) "#+\\s-*")

  (font-lock-fontify-buffer))
