(defconst metacomputer-mode-syntax-table
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

(setq metacomputer-highlights
      '(("\\b\\(module\\|if\\|else\\|fun\\|let\\|val\\|method_base\\|struct\\|return\\|import\\|include\\)\\b" . font-lock-keyword-face)))

(define-derived-mode metacomputer-mode prog-mode "MetaComputer mode"
  :syntax-table metacomputer-mode-syntax-table
  (setq font-lock-defaults '(metacomputer-highlights))

  (set (make-local-variable 'indent-line-function) 'js-indent-line)
  (set (make-local-variable 'comment-start) "# ")
  (set (make-local-variable 'comment-start-skip) "#+\\s-*")

  (font-lock-fontify-buffer))
